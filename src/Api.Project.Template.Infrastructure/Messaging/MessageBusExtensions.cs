using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.RabbitMq;
using Api.Project.Template.Infrastructure.Messaging.ServiceBus;
using Api.Project.Template.Infrastructure.Messaging.Sqs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Extension methods for configuring message bus with routing abstraction.
/// </summary>
public static class MessageBusExtensions
{
    /// <summary>
    /// Adds message bus with automatic provider selection based on configuration.
    /// Registers both IMessagePublisher and IMessageBrokerAdapter for the selected provider.
    /// Provider determined by MessageBus:Routing:Provider setting ("RabbitMq", "ServiceBus", "Auto").
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration root</param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when provider is invalid or required connection strings are missing.
    /// </exception>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind routing configuration
        services.Configure<MessageRoutingConfig>(
            configuration.GetSection("MessageBus:Routing"));

        var routingConfig = configuration
            .GetSection("MessageBus:Routing")
            .Get<MessageRoutingConfig>() ?? new MessageRoutingConfig();

        var provider = ResolveProvider(routingConfig.Provider, configuration);

        // Register publisher and consumer adapter based on provider
        if (provider == "RabbitMq")
        {
            RegisterRabbitMq(services);
        }
        else if (provider == "ServiceBus")
        {
            RegisterServiceBus(services);
        }
        else if (provider == "Sqs")
        {
            RegisterSqs(services);
        }

        return services;
    }

    /// <summary>
    /// Registers a message consumer and its background host for the given message and processor types.
    /// Uses <see cref="MessageConsumerWorker{TMessage,TProcessor}"/> as the hosted service.
    /// </summary>
    /// <typeparam name="TMessage">The message type to consume.</typeparam>
    /// <typeparam name="TProcessor">
    /// The processor class that handles <typeparamref name="TMessage"/>.
    /// Registered as scoped to support per-message dependency injection (e.g. DbContext).
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Call <see cref="AddMessageBus"/> before calling this method.
    /// For a named hosted service (better log output, customizable per service),
    /// use the <c>AddMessageConsumer&lt;TMessage, TProcessor, TWorker&gt;</c> overload instead.
    /// </remarks>
    public static IServiceCollection AddMessageConsumer<TMessage, TProcessor>(
        this IServiceCollection services,
        string consumerSection = GenericMessageConsumer.DefaultConsumerSection)
        where TProcessor : class, IMessageProcessor<TMessage>
        => services.AddMessageConsumer<TMessage, TProcessor, MessageConsumerWorker<TMessage, TProcessor>>(consumerSection);

    /// <summary>
    /// Registers a message consumer and its background host for the given message, processor, and worker types.
    /// </summary>
    /// <typeparam name="TMessage">The message type to consume.</typeparam>
    /// <typeparam name="TProcessor">
    /// The processor class that handles <typeparamref name="TMessage"/>.
    /// Registered as scoped to support per-message dependency injection (e.g. DbContext).
    /// </typeparam>
    /// <typeparam name="TWorker">
    /// A <see cref="MessageConsumerWorker{TMessage,TProcessor}"/> subclass to use as the hosted service.
    /// Allows service-specific log names and lifecycle customization.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="consumerSection">
    /// Configuration section with this consumer's settings (Queue, Concurrency, MaxRetries, PrefetchCount,
    /// RoutingKey, SubscriptionName). Give each consumer its own section when registering more than one;
    /// unset values fall back to <c>MessageBus:Consumer</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Call <see cref="AddMessageBus"/> before calling this method.
    /// Each consumer gets its own <see cref="IMessageBrokerAdapter"/> (and so its own broker connection),
    /// registered under <typeparamref name="TWorker"/> as a key so consumers don't replace each other.
    /// </remarks>
    public static IServiceCollection AddMessageConsumer<TMessage, TProcessor, TWorker>(
        this IServiceCollection services,
        string consumerSection = GenericMessageConsumer.DefaultConsumerSection)
        where TProcessor : class, IMessageProcessor<TMessage>
        where TWorker : MessageConsumerWorker<TMessage, TProcessor>
    {
        services.AddScoped<TProcessor>();

        // Keyed singleton: owned (and disposed) by the container, one per worker type.
        services.AddKeyedSingleton<IMessageConsumer>(typeof(TWorker), (sp, _) =>
            ActivatorUtilities.CreateInstance<GenericMessageConsumer<TMessage, TProcessor>>(sp, consumerSection));

        services.AddHostedService(sp => ActivatorUtilities.CreateInstance<TWorker>(
            sp, sp.GetRequiredKeyedService<IMessageConsumer>(typeof(TWorker))));

        return services;
    }

    /// <summary>
    /// Resolves the provider string to a normalized value, handling auto-detection.
    /// </summary>
    internal static string ResolveProvider(string? providerValue, IConfiguration configuration)
    {
        return providerValue?.ToLowerInvariant() switch
        {
            "rabbitmq" => "RabbitMq",
            "servicebus" => "ServiceBus",
            "sqs" => "Sqs",
            "auto" or null or "" => ProviderFromMessagingProvider(configuration) ?? DetectProvider(configuration),
            _ => throw new InvalidOperationException(
                $"Invalid MessageBus:Routing:Provider value: '{providerValue}'. " +
                $"Valid values: 'RabbitMq', 'ServiceBus', 'Sqs', 'Auto'")
        };
    }

    /// <summary>
    /// Registers RabbitMQ implementations for both publisher and consumer.
    /// Registers IMessagePublisher with RoutingMessagePublisher decorator and IMessageBrokerAdapter with RabbitMqBrokerAdapter.
    /// </summary>
    /// <param name="services">The service collection</param>
    private static void RegisterRabbitMq(IServiceCollection services)
    {
        // Register publisher
        services.AddSingleton<RabbitMqMessagePublisher>();
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var innerPublisher = sp.GetRequiredService<RabbitMqMessagePublisher>();
            var routingOptions = sp.GetRequiredService<IOptions<MessageRoutingConfig>>();
            var logger = sp.GetRequiredService<ILoggerAdapter<RoutingMessagePublisher>>();

            logger.LogInformation("Message bus publisher configured with provider: RabbitMq");

            return new RoutingMessagePublisher(innerPublisher, routingOptions, logger);
        });

        // Register consumer adapter
        services.AddTransient<IMessageBrokerAdapter, RabbitMqBrokerAdapter>();
    }

    /// <summary>
    /// Registers Azure Service Bus implementations for both publisher and consumer.
    /// Registers IMessagePublisher with RoutingMessagePublisher decorator and IMessageBrokerAdapter with ServiceBusBrokerAdapter.
    /// </summary>
    /// <param name="services">The service collection</param>
    private static void RegisterServiceBus(IServiceCollection services)
    {
        // Register publisher
        services.AddSingleton<ServiceBusMessagePublisher>();
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var innerPublisher = sp.GetRequiredService<ServiceBusMessagePublisher>();
            var routingOptions = sp.GetRequiredService<IOptions<MessageRoutingConfig>>();
            var logger = sp.GetRequiredService<ILoggerAdapter<RoutingMessagePublisher>>();

            logger.LogInformation("Message bus publisher configured with provider: ServiceBus");

            return new RoutingMessagePublisher(innerPublisher, routingOptions, logger);
        });

        // Register consumer adapter
        services.AddTransient<IMessageBrokerAdapter, ServiceBusBrokerAdapter>();
    }

    /// <summary>
    /// Registers AWS SQS/SNS implementations for both publisher and consumer.
    /// Registers IMessagePublisher with RoutingMessagePublisher decorator and IMessageBrokerAdapter with SqsBrokerAdapter.
    /// </summary>
    private static void RegisterSqs(IServiceCollection services)
    {
        services.AddSingleton<SqsMessagePublisher>();
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var innerPublisher = sp.GetRequiredService<SqsMessagePublisher>();
            var routingOptions = sp.GetRequiredService<IOptions<MessageRoutingConfig>>();
            var logger = sp.GetRequiredService<ILoggerAdapter<RoutingMessagePublisher>>();

            logger.LogInformation("Message bus publisher configured with provider: Sqs");

            return new RoutingMessagePublisher(innerPublisher, routingOptions, logger);
        });

        services.AddTransient<IMessageBrokerAdapter, SqsBrokerAdapter>();
    }

    /// <summary>
    /// The provider named by the top-level <c>MessagingProvider</c> setting (the one the AppHost passes to
    /// every project), or null when it's unset or "None". Checked before connection-string detection so a
    /// leftover connection string for another broker can't switch providers.
    /// </summary>
    internal static string? ProviderFromMessagingProvider(IConfiguration configuration)
        => configuration["MessagingProvider"]?.ToLowerInvariant() switch
        {
            "rabbitmq" => "RabbitMq",
            "servicebus" => "ServiceBus",
            "sqs" => "Sqs",
            _ => null
        };

    /// <summary>
    /// Detects available provider by checking for connection strings.
    /// </summary>
    internal static string DetectProvider(IConfiguration configuration)
    {
        var hasRabbitMq = HasRabbitMqConnectionString(configuration);
        var hasServiceBus = HasServiceBusConnectionString(configuration);
        var hasSqs = HasSqsConnectionString(configuration);

        if (hasRabbitMq)
            return "RabbitMq";

        if (hasServiceBus)
            return "ServiceBus";

        if (hasSqs)
            return "Sqs";

        throw new InvalidOperationException(
            "No message broker connection string found. " +
            "Set ConnectionStrings:messaging (RabbitMQ), ConnectionStrings:servicebus (Azure Service Bus), " +
            "or ConnectionStrings:sqs (AWS SQS/SNS), " +
            "or explicitly set MessageBus:Routing:Provider.");
    }

    /// <summary>
    /// Checks if any RabbitMQ connection string is configured.
    /// </summary>
    internal static bool HasRabbitMqConnectionString(IConfiguration configuration)
    {
        return !string.IsNullOrEmpty(configuration["MessageBus:RabbitMq:ConnectionString"])
            || !string.IsNullOrEmpty(configuration["MessageBus:RabbitMq"])
            || !string.IsNullOrEmpty(configuration["MessageBus__RabbitMq__ConnectionString"])
            || !string.IsNullOrEmpty(configuration["MessageBus__RabbitMq"])
            || !string.IsNullOrEmpty(configuration.GetConnectionString("RabbitMq"))
            || !string.IsNullOrEmpty(configuration.GetConnectionString("messaging"));
    }

    /// <summary>
    /// Checks if Azure Service Bus connection string is configured.
    /// </summary>
    internal static bool HasServiceBusConnectionString(IConfiguration configuration)
    {
        return !string.IsNullOrEmpty(configuration["ConnectionStrings:servicebus"])
            || !string.IsNullOrEmpty(configuration.GetConnectionString("servicebus"));
    }

    /// <summary>
    /// Checks if an AWS SQS connection string (endpoint URL) is configured.
    /// </summary>
    internal static bool HasSqsConnectionString(IConfiguration configuration)
    {
        return !string.IsNullOrEmpty(configuration["ConnectionStrings:sqs"])
            || !string.IsNullOrEmpty(configuration.GetConnectionString("sqs"));
    }
}
