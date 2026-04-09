using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// RabbitMQ implementation of IMessageBrokerAdapter.
/// Wraps RabbitMQ.Client APIs to provide a consistent interface for message consumption.
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

        // Declare exchange and queue
        await _channel.ExchangeDeclareAsync(exchange, exchangeType, durable: true, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(
            queue: config.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null, cancellationToken: cancellationToken);

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

                    // ACK invalid messages to prevent reprocessing
                    await AckMessageAsync(ea.DeliveryTag);
                    return;
                }

                if (message == null)
                {
                    logger.LogWarning("Deserialized message is null on queue {Queue}", _queueName);
                    await AckMessageAsync(ea.DeliveryTag);
                    return;
                }

                // Create message context
                var context = new MessageContext
                {
                    MessageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString(),
                    CorrelationId = ea.BasicProperties.CorrelationId ?? "",
                    DeliveryCount = ea.Redelivered ? 2 : 1, // RabbitMQ doesn't expose exact count
                    Headers = ConvertHeaders(ea.BasicProperties?.Headers),
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
                else
                {
                    await NackMessageAsync(ea.DeliveryTag, result.Requeue);

                    logger.LogWarning(
                        "Message processing failed (Queue: {Queue}, Requeue: {Requeue}, Reason: {Reason})",
                        _queueName, result.Requeue, result.ErrorReason);
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
