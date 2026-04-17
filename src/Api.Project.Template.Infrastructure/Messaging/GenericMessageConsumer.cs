using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Generic message consumer that works with any broker via IMessageBrokerAdapter.
/// Resolves message processors from DI and delegates message handling to them.
/// </summary>
/// <typeparam name="TMessage">The type of message to consume</typeparam>
/// <typeparam name="TProcessor">The processor type that handles TMessage</typeparam>
public class GenericMessageConsumer<TMessage, TProcessor>(
    IMessageBrokerAdapter adapter,
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILoggerAdapter<GenericMessageConsumer<TMessage, TProcessor>> logger)
    : IMessageConsumer
    where TProcessor : IMessageProcessor<TMessage>
{
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

        var config = new MessageBrokerConfig
        {
            ConnectionString = connectionString,
            Queue = configuration["MessageBus:Consumer:Queue"]
                ?? throw new InvalidOperationException("MessageBus:Consumer:Queue not configured"),
            Concurrency = int.TryParse(configuration["MessageBus:Consumer:Concurrency"], out var concurrency)
                ? concurrency
                : 5,
            MaxRetries = int.TryParse(configuration["MessageBus:Consumer:MaxRetries"], out var maxRetries)
                ? maxRetries
                : 3,
            PrefetchCount = int.TryParse(configuration["MessageBus:Consumer:PrefetchCount"], out var prefetchCount)
                ? prefetchCount
                : 10
        };

        // RabbitMQ-specific configuration
        var exchange = configuration["MessageBus:RabbitMq:Exchange"];
        if (!string.IsNullOrWhiteSpace(exchange))
            config.ProviderSpecific["Exchange"] = exchange;

        var routingKey = configuration["MessageBus:RabbitMq:RoutingKey"];
        if (!string.IsNullOrWhiteSpace(routingKey))
            config.ProviderSpecific["RoutingKey"] = routingKey;

        var exchangeType = configuration["MessageBus:RabbitMq:ExchangeType"];
        if (!string.IsNullOrWhiteSpace(exchangeType))
            config.ProviderSpecific["ExchangeType"] = exchangeType;

        // Azure Service Bus-specific configuration
        var subscriptionName = configuration["MessageBus:ServiceBus:SubscriptionName"];
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
