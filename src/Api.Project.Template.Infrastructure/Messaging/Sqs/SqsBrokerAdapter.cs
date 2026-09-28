using Amazon.SQS;
using Amazon.SQS.Model;
using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.Sqs;

/// <summary>
/// AWS SQS implementation of IMessageBrokerAdapter.
/// Uses long-polling to receive messages and bounds concurrency with handler slots: a batch is only
/// requested for slots that are free, so received messages never wait (invisible) in a local buffer.
/// While a handler runs, the message's visibility timeout is extended so a slow handler isn't redelivered.
/// On success: deletes the message. On retryable failure: sets visibility timeout to 0
/// for immediate redelivery. On terminal failure (non-retryable, out of retries, or
/// undeserializable): copies the message to "{queue}-dlq" with a DeadLetterReason attribute,
/// then deletes the original. If a handler is cancelled because the consumer is stopping, the
/// message is made visible again immediately, without counting as a failure.
/// DisconnectAsync stops polling and waits (bounded) for in-flight handlers to finish.
/// Supports LocalStack for local development (set ConnectionStrings:sqs to the LocalStack endpoint URL).
/// </summary>
public class SqsBrokerAdapter(ILoggerAdapter<SqsBrokerAdapter> logger) : IMessageBrokerAdapter
{
    // How long DisconnectAsync waits for in-flight handlers before disposing anyway.
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(30);

    // Upper bound for settling a message (delete / requeue / dead-letter) once processing has finished.
    // Settling deliberately ignores the shutdown token: a processed message must still be deleted.
    private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(10);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public string ConnectionStringName => "sqs";

    /// <inheritdoc />
    /// <remarks>The connection string is only the endpoint override (LocalStack); real AWS needs none.</remarks>
    public bool RequiresConnectionString => false;

    private readonly ConcurrentDictionary<Task, byte> _inFlight = new();
    private AmazonSQSClient? _sqsClient;
    private string? _queueUrl;
    private string? _deadLetterQueueUrl;
    private bool _disposed;
    private int _concurrency;
    private int _maxRetries;
    private int _visibilityTimeoutSeconds;
    private SemaphoreSlim? _handlerSlots;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    public async Task ConnectAsync(MessageBrokerConfig config, CancellationToken cancellationToken)
    {
        _concurrency = config.Concurrency;
        _maxRetries = config.MaxRetries;
        _handlerSlots = new SemaphoreSlim(_concurrency, _concurrency);

        var region = config.ProviderSpecific.TryGetValue("Region", out var r) ? r : "us-east-1";
        var sqsConfig = new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };

        if (!string.IsNullOrEmpty(config.ConnectionString))
            sqsConfig.ServiceURL = config.ConnectionString;

        if (config.ProviderSpecific.TryGetValue("AccessKey", out var accessKey) &&
            config.ProviderSpecific.TryGetValue("SecretKey", out var secretKey))
        {
            var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            _sqsClient = new AmazonSQSClient(credentials, sqsConfig);
        }
        else
        {
            _sqsClient = new AmazonSQSClient(sqsConfig);
        }

        // CreateQueueAsync is idempotent — returns the existing URL if the queue already exists.
        // This mirrors how RabbitMQ declares queues on connect and avoids a hard dependency on
        // out-of-band queue provisioning in local and CI environments.
        var createResponse = await _sqsClient.CreateQueueAsync(config.Queue, cancellationToken);
        _queueUrl = createResponse.QueueUrl;

        var deadLetterResponse = await _sqsClient.CreateQueueAsync($"{config.Queue}-dlq", cancellationToken);
        _deadLetterQueueUrl = deadLetterResponse.QueueUrl;

        // The queue's visibility timeout drives how often in-flight messages are extended.
        var attributes = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = _queueUrl,
            AttributeNames = ["VisibilityTimeout"]
        }, cancellationToken);
        _visibilityTimeoutSeconds =
            attributes.Attributes?.TryGetValue("VisibilityTimeout", out var timeout) == true &&
            int.TryParse(timeout, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
                ? seconds
                : 30;

        logger.LogInformation(
            "SqsBrokerAdapter connected (Queue: {Queue}, QueueUrl: {QueueUrl}, Concurrency: {Concurrency}, VisibilityTimeout: {VisibilityTimeout}s)",
            config.Queue, _queueUrl, _concurrency, _visibilityTimeoutSeconds);
    }

    public Task SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken)
    {
        if (_sqsClient == null || _queueUrl == null)
            throw new InvalidOperationException("Must call ConnectAsync before SubscribeAsync");

        // Polling stops on either the caller's token or DisconnectAsync; handlers get the caller's token
        // so processors can observe shutdown.
        _pollingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollingTask = Task.Run(() => PollAsync(handler, _pollingCts.Token, cancellationToken), CancellationToken.None);

        logger.LogInformation("SqsBrokerAdapter polling started for queue {QueueUrl}", _queueUrl);

        return Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        if (_pollingCts != null)
        {
            await _pollingCts.CancelAsync();

            if (_pollingTask != null)
            {
                try { await _pollingTask; }
                catch (OperationCanceledException) { }
            }
        }

        // Let in-flight handlers finish (and settle their messages) before the client goes away.
        var inFlight = _inFlight.Keys.ToArray();
        if (inFlight.Length > 0)
        {
            logger.LogInformation("SqsBrokerAdapter waiting for {Count} in-flight message(s) to finish", inFlight.Length);

            var allDone = Task.WhenAll(inFlight);
            if (await Task.WhenAny(allDone, Task.Delay(DrainTimeout)) != allDone)
            {
                logger.LogWarning(
                    "SqsBrokerAdapter stopped waiting after {Timeout}; unfinished messages will be redelivered",
                    DrainTimeout);
            }
        }

        logger.LogInformation("SqsBrokerAdapter disconnected from queue {QueueUrl}", _queueUrl);
    }

    public async ValueTask DisposeAsync()
    {
        // Both the consumer and the DI container dispose this singleton; only the first call does work.
        if (_disposed)
            return;
        _disposed = true;

        await DisconnectAsync();
        _pollingCts?.Dispose();
        _pollingCts = null;
        _sqsClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task PollAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken pollingToken,
        CancellationToken handlerToken)
    {
        var slots = _handlerSlots!;

        while (!pollingToken.IsCancellationRequested)
        {
            // Reserve slots before receiving: at least one, plus any others that are free (max 10 per receive).
            // Receiving more than can be processed would leave messages invisible in a local buffer.
            try
            {
                await slots.WaitAsync(pollingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var reserved = 1;
            while (reserved < Math.Min(10, _concurrency) && slots.Wait(0))
                reserved++;

            List<Message> messages;
            try
            {
                var response = await _sqsClient!.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = reserved,
                    WaitTimeSeconds = 20,
                    MessageSystemAttributeNames = ["ApproximateReceiveCount"],
                    MessageAttributeNames = ["All"]
                }, pollingToken);

                // AWS SDK v4 returns null (not an empty list) when no messages were received
                messages = response.Messages ?? [];
            }
            catch (OperationCanceledException) when (pollingToken.IsCancellationRequested)
            {
                slots.Release(reserved);
                break;
            }
            catch (Exception ex)
            {
                slots.Release(reserved);
                logger.LogError(ex, "Error receiving messages from SQS queue {QueueUrl}", _queueUrl);
                try { await Task.Delay(TimeSpan.FromSeconds(5), pollingToken); }
                catch (OperationCanceledException) { break; }
                continue;
            }

            // Give back the slots this receive didn't fill
            if (reserved > messages.Count)
                slots.Release(reserved - messages.Count);

            foreach (var sqsMessage in messages)
            {
                var task = Task.Run(async () =>
                {
                    try { await HandleMessageAsync(sqsMessage, handler, handlerToken); }
                    catch (Exception ex)
                    {
                        // Settling failed (e.g. network error); the message will reappear after its visibility timeout.
                        logger.LogError(ex, "Error settling SQS message {MessageId}", sqsMessage.MessageId);
                    }
                    finally { slots.Release(); }
                }, CancellationToken.None);

                _inFlight.TryAdd(task, 0);
                _ = task.ContinueWith(t => _inFlight.TryRemove(t, out _), TaskScheduler.Default);
            }
        }
    }

    private async Task HandleMessageAsync<TMessage>(
        Message sqsMessage,
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken)
    {
        // Message attributes as strings; for an SNS notification, the payload and attributes are inside the envelope
        var body = sqsMessage.Body;
        var messageAttributes = (sqsMessage.MessageAttributes ?? [])
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.StringValue ?? "");

        if (SnsEnvelope.TryUnwrap(body, out var snsPayload, out var snsAttributes))
        {
            body = snsPayload;
            foreach (var (key, value) in snsAttributes)
                messageAttributes[key] = value;
        }

        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(body, _jsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex,
                "Failed to deserialize SQS message {MessageId}: {Body}",
                sqsMessage.MessageId, sqsMessage.Body);
            await DeadLetterAsync(sqsMessage, "DeserializationFailed");
            return;
        }

        if (message == null)
        {
            logger.LogWarning("Deserialized SQS message {MessageId} is null", sqsMessage.MessageId);
            await DeadLetterAsync(sqsMessage, "NullMessage");
            return;
        }

        // AWS SDK v4 leaves collections null when the response omits them
        string? receiveCountStr = null;
        sqsMessage.Attributes?.TryGetValue("ApproximateReceiveCount", out receiveCountStr);
        _ = int.TryParse(receiveCountStr, out var deliveryCount);

        var context = new MessageContext
        {
            MessageId = sqsMessage.MessageId,
            CorrelationId = messageAttributes.GetValueOrDefault("CorrelationId", ""),
            DeliveryCount = deliveryCount,
            Headers = messageAttributes.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value),
            CancellationToken = cancellationToken
        };

        MessageProcessingResult result;
        using (var heartbeatCts = new CancellationTokenSource())
        {
            var heartbeat = ExtendVisibilityAsync(sqsMessage, heartbeatCts.Token);
            try
            {
                result = await handler(message, context);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The consumer is stopping: hand the message straight back instead of treating it as a failure
                await heartbeatCts.CancelAsync();
                await heartbeat;
                await RequeueAsync(sqsMessage);
                logger.LogInformation("SQS message {MessageId} returned to the queue (consumer stopping)", sqsMessage.MessageId);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unhandled exception in handler for SQS message {MessageId}",
                    sqsMessage.MessageId);
                result = MessageProcessingResult.FailedWithException(ex, requeue: false);
            }
            finally
            {
                await heartbeatCts.CancelAsync();
                await heartbeat;
            }
        }

        if (result.Success)
        {
            using var settle = new CancellationTokenSource(SettleTimeout);
            await _sqsClient!.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle, settle.Token);

            logger.LogDebug("Deleted SQS message {MessageId} after successful processing", sqsMessage.MessageId);
        }
        else if (result.Requeue && deliveryCount < _maxRetries)
        {
            await RequeueAsync(sqsMessage);

            logger.LogWarning(
                "SQS message {MessageId} requeued (DeliveryCount: {DeliveryCount}, Reason: {Reason})",
                sqsMessage.MessageId, deliveryCount, result.ErrorReason);
        }
        else
        {
            // Exceeded retries or non-retryable
            await DeadLetterAsync(sqsMessage, result.ErrorReason ?? "ProcessingFailed");

            logger.LogWarning(
                "SQS message {MessageId} dead-lettered after {DeliveryCount} deliveries (Reason: {Reason})",
                sqsMessage.MessageId, deliveryCount, result.ErrorReason);
        }
    }

    /// <summary>
    /// Keeps the message invisible while its handler runs by extending the visibility timeout
    /// at half-timeout intervals. Without this, a handler slower than the timeout lets SQS redeliver
    /// the message to another receiver while it's still being processed.
    /// </summary>
    private async Task ExtendVisibilityAsync(Message sqsMessage, CancellationToken stopToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _visibilityTimeoutSeconds / 2.0));
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stopToken))
            {
                try
                {
                    await _sqsClient!.ChangeMessageVisibilityAsync(
                        _queueUrl, sqsMessage.ReceiptHandle, _visibilityTimeoutSeconds, stopToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to extend visibility of SQS message {MessageId}", sqsMessage.MessageId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Handler finished
        }
    }

    /// <summary>
    /// Makes the message visible again immediately (visibility timeout 0).
    /// </summary>
    private async Task RequeueAsync(Message sqsMessage)
    {
        using var settle = new CancellationTokenSource(SettleTimeout);
        await _sqsClient!.ChangeMessageVisibilityAsync(_queueUrl, sqsMessage.ReceiptHandle, 0, settle.Token);
    }

    /// <summary>
    /// Copies the message to the dead-letter queue, then deletes the original.
    /// SQS has no "dead-letter now" API (a redrive policy only acts after maxReceiveCount receives),
    /// so the move is done explicitly. If the send fails, the original is left for redelivery.
    /// </summary>
    private async Task DeadLetterAsync(Message sqsMessage, string reason)
    {
        using var settle = new CancellationTokenSource(SettleTimeout);

        var attributes = new Dictionary<string, MessageAttributeValue>(sqsMessage.MessageAttributes ?? [])
        {
            // SQS attribute values can't be empty strings
            ["DeadLetterReason"] = new() { DataType = "String", StringValue = string.IsNullOrEmpty(reason) ? "Unknown" : reason }
        };

        await _sqsClient!.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _deadLetterQueueUrl,
            MessageBody = sqsMessage.Body,
            MessageAttributes = attributes
        }, settle.Token);

        await _sqsClient.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle, settle.Token);
    }
}
