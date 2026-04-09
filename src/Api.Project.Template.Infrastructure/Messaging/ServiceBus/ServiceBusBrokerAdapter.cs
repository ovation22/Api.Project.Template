using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using System.Text;
using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.ServiceBus;

/// <summary>
/// Azure Service Bus implementation of IMessageBrokerAdapter.
/// Wraps Azure.Messaging.ServiceBus APIs to provide a consistent interface for message consumption.
/// </summary>
public class ServiceBusBrokerAdapter(ILoggerAdapter<ServiceBusBrokerAdapter> logger) : IMessageBrokerAdapter
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;
    private string? _queueName;
    private int _maxRetries;

    /// <inheritdoc/>
    /// <remarks>
    /// For Azure Service Bus, this method creates the <see cref="ServiceBusClient"/> and configures
    /// the <see cref="ServiceBusProcessor"/>, but does NOT open a connection to the broker.
    /// The underlying connection is deferred to <see cref="SubscribeAsync{TMessage}"/>, which calls
    /// <c>ServiceBusProcessor.StartProcessingAsync</c> — the first point at which the SDK connects.
    /// This is an SDK constraint: <see cref="ServiceBusProcessor"/> has no standalone connect method.
    /// </remarks>
    public async Task ConnectAsync(MessageBrokerConfig config, CancellationToken cancellationToken)
    {
        _queueName = config.Queue;
        _maxRetries = config.MaxRetries;

        _client = new ServiceBusClient(config.ConnectionString);

        _processor = _client.CreateProcessor(
            config.Queue,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = config.Concurrency,
                PrefetchCount = config.PrefetchCount
            });

        logger.LogInformation(
            "ServiceBusBrokerAdapter configured (Queue: {Queue}, Concurrency: {Concurrency}) — connection deferred to SubscribeAsync",
            config.Queue, config.Concurrency);

        await Task.CompletedTask;
    }

    public async Task SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken)
    {
        if (_processor == null)
            throw new InvalidOperationException("Must call ConnectAsync before SubscribeAsync");

        // Wire up message handler
        _processor.ProcessMessageAsync += async args =>
        {
            try
            {
                // Deserialize message
                var body = Encoding.UTF8.GetString(args.Message.Body);
                TMessage? message;

                try
                {
                    message = JsonSerializer.Deserialize<TMessage>(body, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex,
                        "Failed to deserialize message on queue {Queue}: {Body}",
                        _queueName, body);

                    // Dead-letter invalid messages
                    await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", ex.Message, cancellationToken);
                    return;
                }

                if (message == null)
                {
                    logger.LogWarning("Deserialized message is null on queue {Queue}", _queueName);
                    await args.DeadLetterMessageAsync(args.Message, "NullMessage", "Message deserialized to null", cancellationToken);
                    return;
                }

                // Create message context
                var context = new MessageContext
                {
                    MessageId = args.Message.MessageId,
                    CorrelationId = args.Message.CorrelationId ?? "",
                    DeliveryCount = args.Message.DeliveryCount,
                    Headers = ConvertApplicationProperties(args.Message.ApplicationProperties),
                    CancellationToken = args.CancellationToken
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
                        "Unhandled exception in message handler for queue {Queue}, MessageId {MessageId}",
                        _queueName, args.Message.MessageId);
                    result = MessageProcessingResult.FailedWithException(ex, requeue: false);
                }

                // Handle result
                if (result.Success)
                {
                    await args.CompleteMessageAsync(args.Message, cancellationToken);
                }
                else
                {
                    // Check if we've exceeded max retries
                    if (args.Message.DeliveryCount >= _maxRetries)
                    {
                        logger.LogWarning(
                            "Message exceeded max retries (Queue: {Queue}, MessageId: {MessageId}, DeliveryCount: {DeliveryCount})",
                            _queueName, args.Message.MessageId, args.Message.DeliveryCount);

                        await args.DeadLetterMessageAsync(
                            args.Message,
                            "MaxRetriesExceeded",
                            result.ErrorReason ?? "Processing failed after maximum retries", cancellationToken);
                    }
                    else if (result.Requeue)
                    {
                        // Abandon to requeue
                        await args.AbandonMessageAsync(args.Message, cancellationToken: cancellationToken);

                        logger.LogWarning(
                            "Message processing failed, requeuing (Queue: {Queue}, MessageId: {MessageId}, Reason: {Reason})",
                            _queueName, args.Message.MessageId, result.ErrorReason);
                    }
                    else
                    {
                        // Dead-letter without retrying
                        await args.DeadLetterMessageAsync(
                            args.Message,
                            "ProcessingFailed",
                            result.ErrorReason ?? "Processing failed", cancellationToken);

                        logger.LogWarning(
                            "Message processing failed, dead-lettered (Queue: {Queue}, MessageId: {MessageId}, Reason: {Reason})",
                            _queueName, args.Message.MessageId, result.ErrorReason);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Unexpected error processing message (Queue: {Queue}, MessageId: {MessageId})",
                    _queueName, args.Message.MessageId);

                // Dead-letter on unexpected errors
                try
                {
                    await args.DeadLetterMessageAsync(args.Message, "UnexpectedError", ex.Message, cancellationToken);
                }
                catch (Exception deadLetterEx)
                {
                    logger.LogError(deadLetterEx, "Failed to dead-letter message {MessageId}", args.Message.MessageId);
                }
            }
        };

        // Wire up error handler
        _processor.ProcessErrorAsync += async args =>
        {
            logger.LogError(args.Exception,
                "Service Bus error: {ErrorSource}, Entity: {EntityPath}",
                args.ErrorSource,
                args.EntityPath);

            await Task.CompletedTask;
        };

        // Start processing
        await _processor.StartProcessingAsync(cancellationToken);

        logger.LogInformation("ServiceBusBrokerAdapter subscribed to queue {Queue}", _queueName);
    }

    public async Task DisconnectAsync()
    {
        if (_processor != null)
        {
            try
            {
                await _processor.StopProcessingAsync();
                logger.LogInformation("ServiceBusBrokerAdapter disconnected from queue {Queue}", _queueName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error stopping processor");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();

        if (_processor != null)
        {
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    private static IReadOnlyDictionary<string, object> ConvertApplicationProperties(
        IReadOnlyDictionary<string, object> applicationProperties)
    {
        return new Dictionary<string, object>(applicationProperties);
    }
}
