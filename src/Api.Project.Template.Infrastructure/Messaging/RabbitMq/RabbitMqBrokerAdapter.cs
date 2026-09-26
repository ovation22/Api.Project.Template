using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// RabbitMQ implementation of IMessageBrokerAdapter.
/// Wraps RabbitMQ.Client APIs to provide a consistent interface for message consumption.
/// Retryable failures are republished with an x-retry-count header until MaxRetries is reached;
/// everything else is dead-lettered to "{queue}.dlq" via the "{queue}.dlx" exchange.
/// </summary>
public class RabbitMqBrokerAdapter(ILoggerAdapter<RabbitMqBrokerAdapter> logger) : IMessageBrokerAdapter
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private SemaphoreSlim? _semaphore;
    private string? _queueName;
    private int _maxRetries;
    private bool _disposed;

    // Classic queues don't track delivery attempts, so retries are counted in this header.
    private const string RetryCountHeader = "x-retry-count";

    public async Task ConnectAsync(MessageBrokerConfig config, CancellationToken cancellationToken)
    {
        _queueName = config.Queue;

        // Parse connection string
        var factory = RabbitMqConnectionStringParser.Parse(config.ConnectionString);

        // Set resilience settings
        factory.AutomaticRecoveryEnabled = true;
        factory.TopologyRecoveryEnabled = true;
        factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
        factory.RequestedHeartbeat = TimeSpan.FromSeconds(30);

        // Create connection and channel
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Get RabbitMQ-specific configuration
        var exchange = config.ProviderSpecific.GetValueOrDefault("Exchange", "apiprojecttemplate.events");
        var routingKey = config.ProviderSpecific.GetValueOrDefault("RoutingKey", "");
        var exchangeType = config.ProviderSpecific.GetValueOrDefault("ExchangeType", ExchangeType.Topic);

        // Declare the dead-letter exchange and queue: failed messages are routed here
        // (via x-dead-letter-exchange) instead of being dropped.
        var deadLetterExchange = $"{config.Queue}.dlx";
        var deadLetterQueue = $"{config.Queue}.dlq";
        await _channel.ExchangeDeclareAsync(deadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(deadLetterQueue, deadLetterExchange, routingKey: "", cancellationToken: cancellationToken);

        // Declare exchange and queue
        await _channel.ExchangeDeclareAsync(exchange, exchangeType, durable: true, cancellationToken: cancellationToken);
        try
        {
            await _channel.QueueDeclareAsync(
                queue: config.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = deadLetterExchange },
                cancellationToken: cancellationToken);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == Constants.PreconditionFailed)
        {
            // RabbitMQ cannot change the arguments of an existing queue.
            throw new InvalidOperationException(
                $"Queue '{config.Queue}' already exists without dead-letter settings. " +
                "Delete it once (e.g. in the management UI) so it can be re-declared with x-dead-letter-exchange.", ex);
        }

        _maxRetries = config.MaxRetries;

        await _channel.QueueBindAsync(
            queue: config.Queue,
            exchange: exchange,
            routingKey: routingKey, cancellationToken: cancellationToken);

        // Set QoS prefetch based on concurrency
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: (ushort)config.Concurrency,
            global: false, cancellationToken: cancellationToken);

        // Initialize semaphore for concurrency control
        _semaphore = new SemaphoreSlim(config.Concurrency, config.Concurrency);

        logger.LogInformation(
            "RabbitMqBrokerAdapter connected (Queue: {Queue}, Exchange: {Exchange}, Concurrency: {Concurrency})",
            config.Queue, exchange, config.Concurrency);
    }

    public async Task SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized. Call ConnectAsync first.");

        if (_semaphore == null)
            throw new InvalidOperationException("Semaphore not initialized. Call ConnectAsync first.");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                // Deserialize message
                var body = ea.Body.ToArray();
                var payload = Encoding.UTF8.GetString(body);
                TMessage? message;

                try
                {
                    message = JsonSerializer.Deserialize<TMessage>(payload, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex,
                        "Failed to deserialize message on queue {Queue}: {Payload}",
                        _queueName, payload);

                    // Dead-letter invalid messages so they can be inspected
                    await NackMessageAsync(ea.DeliveryTag, requeue: false);
                    return;
                }

                if (message == null)
                {
                    logger.LogWarning("Deserialized message is null on queue {Queue}", _queueName);
                    await NackMessageAsync(ea.DeliveryTag, requeue: false);
                    return;
                }

                var retryCount = GetRetryCount(ea.BasicProperties.Headers);

                // Create message context
                var context = new MessageContext
                {
                    MessageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString(),
                    CorrelationId = ea.BasicProperties.CorrelationId ?? "",
                    DeliveryCount = retryCount + 1,
                    Headers = ConvertHeaders(ea.BasicProperties.Headers),
                    CancellationToken = cancellationToken
                };

                // Call handler
                MessageProcessingResult result;
                try
                {
                    result = await handler(message, context);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Unhandled exception in message handler for queue {Queue}, DeliveryTag {DeliveryTag}",
                        _queueName, ea.DeliveryTag);
                    result = MessageProcessingResult.FailedWithException(ex, requeue: false);
                }

                // Handle result
                if (result.Success)
                {
                    await AckMessageAsync(ea.DeliveryTag);
                }
                else if (result.Requeue && context.DeliveryCount < _maxRetries)
                {
                    // Republish with an incremented retry count, then ack the original.
                    // (A plain nack+requeue can't carry a count and would retry forever.)
                    await RepublishForRetryAsync(ea, retryCount + 1);

                    logger.LogWarning(
                        "Message processing failed, retrying (Queue: {Queue}, DeliveryCount: {DeliveryCount}, Reason: {Reason})",
                        _queueName, context.DeliveryCount, result.ErrorReason);
                }
                else
                {
                    // Non-retryable or out of retries: nack without requeue → dead-letter queue
                    await NackMessageAsync(ea.DeliveryTag, requeue: false);

                    logger.LogWarning(
                        "Message dead-lettered (Queue: {Queue}, DeliveryCount: {DeliveryCount}, Reason: {Reason})",
                        _queueName, context.DeliveryCount, result.ErrorReason);
                }
            }
            catch (OperationCanceledException)
            {
                // Requeue on cancellation
                await NackMessageAsync(ea.DeliveryTag, requeue: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error processing message (Queue: {Queue}, DeliveryTag: {DeliveryTag})",
                    _queueName, ea.DeliveryTag);

                // Don't requeue on unexpected errors
                await NackMessageAsync(ea.DeliveryTag, requeue: false);
            }
            finally
            {
                _semaphore.Release();
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _queueName!,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("RabbitMqBrokerAdapter subscribed to queue {Queue}", _queueName);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error closing channel");
        }

        try
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error closing connection");
        }

        logger.LogInformation("RabbitMqBrokerAdapter disconnected");
    }

    public async ValueTask DisposeAsync()
    {
        // Both the consumer and the DI container dispose this singleton; only the first call does work.
        if (_disposed)
            return;
        _disposed = true;

        await DisconnectAsync();

        _semaphore?.Dispose();
        _channelLock.Dispose();

        try { _channel?.Dispose(); } catch { }
        try { _connection?.Dispose(); } catch { }

        GC.SuppressFinalize(this);
    }

    // Private helper methods

    private async Task AckMessageAsync(ulong deliveryTag)
    {
        await _channelLock.WaitAsync();
        try
        {
            await _channel!.BasicAckAsync(deliveryTag, multiple: false);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task NackMessageAsync(ulong deliveryTag, bool requeue)
    {
        await _channelLock.WaitAsync();
        try
        {
            await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue);
        }
        catch { }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task RepublishForRetryAsync(BasicDeliverEventArgs ea, int retryCount)
    {
        var properties = new BasicProperties(ea.BasicProperties)
        {
            Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object?>())
            {
                [RetryCountHeader] = retryCount
            }
        };

        await _channelLock.WaitAsync();
        try
        {
            // Default exchange + queue name as routing key delivers straight back to this queue.
            // Publish before ack: a crash in between redelivers rather than loses the message.
            await _channel!.BasicPublishAsync(
                exchange: "",
                routingKey: _queueName!,
                mandatory: false,
                basicProperties: properties,
                body: ea.Body);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers == null || !headers.TryGetValue(RetryCountHeader, out var value))
            return 0;

        return value switch
        {
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            _ => 0
        };
    }

    private static IReadOnlyDictionary<string, object> ConvertHeaders(IDictionary<string, object?>? headers)
    {
        if (headers == null || headers.Count == 0)
            return new Dictionary<string, object>();

        var result = new Dictionary<string, object>();
        foreach (var header in headers)
        {
            if (header.Value != null)
            {
                // Convert byte[] headers to strings
                if (header.Value is byte[] bytes)
                {
                    result[header.Key] = Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    result[header.Key] = header.Value;
                }
            }
        }

        return result;
    }
}
