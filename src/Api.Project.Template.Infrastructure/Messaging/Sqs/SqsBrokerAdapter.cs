using Amazon.SQS;
using Amazon.SQS.Model;
using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.Sqs;

/// <summary>
/// AWS SQS implementation of IMessageBrokerAdapter.
/// Uses long-polling to receive messages and a semaphore to bound concurrency.
/// On success: deletes the message. On retryable failure: sets visibility timeout to 0
/// for immediate redelivery. On terminal failure: deletes the message — configure a
/// DLQ redrive policy on the queue for dead-lettering.
/// Supports LocalStack for local development (set ConnectionStrings:sqs to the LocalStack endpoint URL).
/// </summary>
public class SqsBrokerAdapter(ILoggerAdapter<SqsBrokerAdapter> logger) : IMessageBrokerAdapter
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private AmazonSQSClient? _sqsClient;
    private string? _queueUrl;
    private int _concurrency;
    private int _maxRetries;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    public async Task ConnectAsync(MessageBrokerConfig config, CancellationToken cancellationToken)
    {
        _concurrency = config.Concurrency;
        _maxRetries = config.MaxRetries;

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

        logger.LogInformation(
            "SqsBrokerAdapter connected (Queue: {Queue}, QueueUrl: {QueueUrl}, Concurrency: {Concurrency})",
            config.Queue, _queueUrl, _concurrency);
    }

    public Task SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken)
    {
        if (_sqsClient == null || _queueUrl == null)
            throw new InvalidOperationException("Must call ConnectAsync before SubscribeAsync");

        _pollingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollingTask = Task.Run(() => PollAsync(handler, _pollingCts.Token), _pollingCts.Token);

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

        logger.LogInformation("SqsBrokerAdapter disconnected from queue {QueueUrl}", _queueUrl);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _pollingCts?.Dispose();
        _sqsClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task PollAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(_concurrency, _concurrency);

        while (!cancellationToken.IsCancellationRequested)
        {
            ReceiveMessageResponse response;
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = Math.Min(10, _concurrency),
                    WaitTimeSeconds = 20,
                    MessageSystemAttributeNames = ["ApproximateReceiveCount"],
                    MessageAttributeNames = ["All"]
                };

                response = await _sqsClient!.ReceiveMessageAsync(request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving messages from SQS queue {QueueUrl}", _queueUrl);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                continue;
            }

            foreach (var sqsMessage in response.Messages)
            {
                await semaphore.WaitAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    try { await HandleMessageAsync(sqsMessage, handler, cancellationToken); }
                    finally { semaphore.Release(); }
                }, cancellationToken);
            }
        }
    }

    private async Task HandleMessageAsync<TMessage>(
        Message sqsMessage,
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken)
    {
        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(sqsMessage.Body, _jsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex,
                "Failed to deserialize SQS message {MessageId}: {Body}",
                sqsMessage.MessageId, sqsMessage.Body);
            await _sqsClient!.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle, cancellationToken);
            return;
        }

        if (message == null)
        {
            logger.LogWarning("Deserialized SQS message {MessageId} is null", sqsMessage.MessageId);
            await _sqsClient!.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle, cancellationToken);
            return;
        }

        sqsMessage.Attributes.TryGetValue("ApproximateReceiveCount", out var receiveCountStr);
        _ = int.TryParse(receiveCountStr, out var deliveryCount);

        var context = new MessageContext
        {
            MessageId = sqsMessage.MessageId,
            CorrelationId = sqsMessage.MessageAttributes.TryGetValue("CorrelationId", out var correlationAttr)
                ? correlationAttr.StringValue ?? ""
                : "",
            DeliveryCount = deliveryCount,
            Headers = sqsMessage.MessageAttributes.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)(kvp.Value.StringValue ?? "")),
            CancellationToken = cancellationToken
        };

        MessageProcessingResult result;
        try
        {
            result = await handler(message, context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception in handler for SQS message {MessageId}",
                sqsMessage.MessageId);
            result = MessageProcessingResult.FailedWithException(ex, requeue: false);
        }

        if (result.Success)
        {
            await _sqsClient!.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle, cancellationToken);

            logger.LogDebug("Deleted SQS message {MessageId} after successful processing", sqsMessage.MessageId);
        }
        else if (result.Requeue && deliveryCount < _maxRetries)
        {
            // Visibility timeout = 0 makes the message immediately available for redelivery
            await _sqsClient!.ChangeMessageVisibilityAsync(_queueUrl, sqsMessage.ReceiptHandle, 0, cancellationToken);

            logger.LogWarning(
                "SQS message {MessageId} requeued (DeliveryCount: {DeliveryCount}, Reason: {Reason})",
                sqsMessage.MessageId, deliveryCount, result.ErrorReason);
        }
        else
        {
            // Exceeded retries or non-retryable — delete and rely on DLQ redrive if configured
            await _sqsClient!.DeleteMessageAsync(_queueUrl, sqsMessage.ReceiptHandle, cancellationToken);

            logger.LogWarning(
                "SQS message {MessageId} discarded after {DeliveryCount} deliveries (Reason: {Reason}). Configure a DLQ redrive policy for dead-lettering.",
                sqsMessage.MessageId, deliveryCount, result.ErrorReason);
        }
    }
}
