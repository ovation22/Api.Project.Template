using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Non-generic constants for <see cref="GenericMessageConsumer{TMessage, TProcessor}"/>.
/// </summary>
public static class GenericMessageConsumer
{
    /// <summary>
    /// The shared consumer settings section; also the fallback for per-consumer sections.
    /// </summary>
    public const string DefaultConsumerSection = "MessageBus:Consumer";
}

/// <summary>
/// Generic message consumer that works with any broker via IMessageBrokerAdapter.
/// Resolves message processors from DI and delegates message handling to them.
/// </summary>
/// <typeparam name="TMessage">The type of message to consume</typeparam>
/// <typeparam name="TProcessor">The processor type that handles TMessage</typeparam>
/// <param name="consumerSection">
/// Configuration section with this consumer's settings (Queue, Concurrency, MaxRetries, PrefetchCount,
/// RoutingKey, SubscriptionName). Unset values fall back to <c>MessageBus:Consumer</c>.
/// </param>
public class GenericMessageConsumer<TMessage, TProcessor>(
    IMessageBrokerAdapter adapter,
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILoggerAdapter<GenericMessageConsumer<TMessage, TProcessor>> logger,
    string consumerSection = GenericMessageConsumer.DefaultConsumerSection)
    : IMessageConsumer
    where TProcessor : IMessageProcessor<TMessage>
{
    private const string DefaultConsumerSection = GenericMessageConsumer.DefaultConsumerSection;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var config = BuildConfiguration();

        await adapter.ConnectAsync(config, cancellationToken);
        await adapter.SubscribeAsync<TMessage>(ProcessMessageAsync, cancellationToken);

        logger.LogInformation(
            "GenericMessageConsumer<{MessageType}, {ProcessorType}> started (Queue: {Queue})",
            typeof(TMessage).Name,
            typeof(TProcessor).Name,
            config.Queue);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await adapter.DisconnectAsync();

        logger.LogInformation(
            "GenericMessageConsumer<{MessageType}, {ProcessorType}> stopped",
            typeof(TMessage).Name,
            typeof(TProcessor).Name);
    }

    public async ValueTask DisposeAsync()
    {
        await adapter.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private async Task<MessageProcessingResult> ProcessMessageAsync(
        TMessage message,
        MessageContext context)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<TProcessor>();

            logger.LogDebug(
                "Processing {MessageType} (MessageId: {MessageId}, CorrelationId: {CorrelationId})",
                typeof(TMessage).Name,
                context.MessageId,
                context.CorrelationId);

            var result = await processor.ProcessAsync(message, context);

            if (result.Success)
            {
                logger.LogInformation(
                    "Successfully processed {MessageType} (MessageId: {MessageId})",
                    typeof(TMessage).Name,
                    context.MessageId);
            }
            else
            {
                logger.LogWarning(
                    "Failed to process {MessageType} (MessageId: {MessageId}, Reason: {Reason})",
                    typeof(TMessage).Name,
                    context.MessageId,
                    result.ErrorReason);
            }

            return result;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // The consumer is stopping. Let the adapter return the message to the queue
            // rather than turning shutdown into a processing failure (which would dead-letter it).
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception processing {MessageType} (MessageId: {MessageId})",
                typeof(TMessage).Name,
                context.MessageId);

            return MessageProcessingResult.FailedWithException(ex, requeue: false);
        }
    }

    private MessageBrokerConfig BuildConfiguration()
    {
        // Aspire injects broker connection strings under ConnectionStrings:<resource-name>.
        // RabbitMQ resource is named "messaging"; Azure Service Bus resource is named "servicebus"; LocalStack is named "sqs".
        var connectionString =
            configuration.GetConnectionString("messaging")
            ?? configuration.GetConnectionString("servicebus")
            ?? configuration.GetConnectionString("sqs")
            ?? throw new InvalidOperationException(
                "Message broker connection string not configured. " +
                "Aspire should inject ConnectionStrings:messaging (RabbitMQ), ConnectionStrings:servicebus (Azure Service Bus), " +
                "or ConnectionStrings:sqs (AWS SQS/LocalStack).");

        // Per-consumer settings come from this consumer's section, falling back to the shared
        // MessageBus:Consumer section (which is also the default section).
        string? Setting(string key) => configuration[$"{consumerSection}:{key}"] ?? configuration[$"{DefaultConsumerSection}:{key}"];

        var config = new MessageBrokerConfig
        {
            ConnectionString = connectionString,
            Queue = configuration[$"{consumerSection}:Queue"]
                ?? throw new InvalidOperationException($"{consumerSection}:Queue not configured"),
            Concurrency = int.TryParse(Setting("Concurrency"), out var concurrency)
                ? concurrency
                : 5,
            MaxRetries = int.TryParse(Setting("MaxRetries"), out var maxRetries)
                ? maxRetries
                : 3,
            PrefetchCount = int.TryParse(Setting("PrefetchCount"), out var prefetchCount)
                ? prefetchCount
                : 10
        };

        // RabbitMQ-specific configuration (the routing key can be set per consumer)
        var exchange = configuration["MessageBus:RabbitMq:Exchange"];
        if (!string.IsNullOrWhiteSpace(exchange))
            config.ProviderSpecific["Exchange"] = exchange;

        var routingKey = configuration[$"{consumerSection}:RoutingKey"] ?? configuration["MessageBus:RabbitMq:RoutingKey"];
        if (!string.IsNullOrWhiteSpace(routingKey))
            config.ProviderSpecific["RoutingKey"] = routingKey;

        var exchangeType = configuration["MessageBus:RabbitMq:ExchangeType"];
        if (!string.IsNullOrWhiteSpace(exchangeType))
            config.ProviderSpecific["ExchangeType"] = exchangeType;

        // Azure Service Bus-specific configuration (Topic set = consume a topic subscription instead of a queue)
        var topic = configuration["MessageBus:ServiceBus:Topic"];
        if (!string.IsNullOrWhiteSpace(topic))
            config.ProviderSpecific["Topic"] = topic;

        var subscriptionName = configuration[$"{consumerSection}:SubscriptionName"] ?? configuration["MessageBus:ServiceBus:SubscriptionName"];
        if (!string.IsNullOrWhiteSpace(subscriptionName))
            config.ProviderSpecific["SubscriptionName"] = subscriptionName;

        // AWS SQS-specific configuration
        var sqsRegion = configuration["MessageBus:Sqs:Region"] ?? configuration["AWS:Region"];
        if (!string.IsNullOrWhiteSpace(sqsRegion))
            config.ProviderSpecific["Region"] = sqsRegion;

        var accessKey = configuration["AWS:AccessKey"];
        if (!string.IsNullOrWhiteSpace(accessKey))
            config.ProviderSpecific["AccessKey"] = accessKey;

        var secretKey = configuration["AWS:SecretKey"];
        if (!string.IsNullOrWhiteSpace(secretKey))
            config.ProviderSpecific["SecretKey"] = secretKey;

        return config;
    }
}
