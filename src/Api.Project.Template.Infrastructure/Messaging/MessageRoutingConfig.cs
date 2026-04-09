namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Configuration for message routing, independent of broker implementation.
/// Mapped from appsettings.json MessageBus:Routing section.
/// </summary>
public class MessageRoutingConfig
{
    /// <summary>
    /// Active message broker provider.
    /// Valid values: "RabbitMq", "ServiceBus", "Auto"
    /// When "Auto", the provider is detected from available connection strings.
    /// </summary>
    public string Provider { get; init; } = "Auto";

    /// <summary>
    /// Default destination if message type not found in Routes.
    /// Maps to RabbitMQ exchange or Azure Service Bus queue/topic.
    /// </summary>
    public string? DefaultDestination { get; init; }

    /// <summary>
    /// Default routing key for RabbitMQ if not specified per message.
    /// Used as fallback when message type has no explicit RoutingKey.
    /// </summary>
    public string? DefaultRoutingKey { get; init; }

    /// <summary>
    /// Per-message-type routing configuration.
    /// Key: Message type name (e.g., "WeatherRequested")
    /// Value: Route configuration for that message
    /// </summary>
    public Dictionary<string, MessageRoute> Routes { get; init; } = new();
}

/// <summary>
/// Routing configuration for a specific message type.
/// </summary>
public class MessageRoute
{
    /// <summary>
    /// Destination name: RabbitMQ exchange, Azure Service Bus queue/topic.
    /// If null, uses MessageRoutingConfig.DefaultDestination.
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// Routing key for RabbitMQ topic exchanges.
    /// Maps to MessagePublishOptions.Subject for broker abstraction.
    /// </summary>
    public string? RoutingKey { get; init; }

    /// <summary>
    /// Subject for Azure Service Bus filtering.
    /// Maps to MessagePublishOptions.Subject for broker abstraction.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Additional provider-specific metadata.
    /// Can be used for custom headers or properties.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
