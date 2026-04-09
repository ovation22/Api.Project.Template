namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Provider-agnostic message broker configuration.
/// Used to configure connection and behavior for any message broker implementation.
/// </summary>
public class MessageBrokerConfig
{
    /// <summary>
    /// Connection string in broker-specific format.
    /// Examples:
    /// - RabbitMQ: "amqp://guest:guest@localhost:5672/" or "Host=localhost;Username=guest;Password=guest"
    /// - Azure Service Bus: "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=..."
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Queue or topic name to consume messages from.
    /// </summary>
    public string Queue { get; set; } = string.Empty;

    /// <summary>
    /// Number of concurrent message processors.
    /// Controls how many messages can be processed in parallel.
    /// Default: 5
    /// </summary>
    public int Concurrency { get; set; } = 5;

    /// <summary>
    /// Maximum number of retry attempts before dead-lettering a message.
    /// Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Number of messages to prefetch and buffer locally.
    /// Higher values improve throughput but increase memory usage.
    /// Default: 10
    /// </summary>
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Provider-specific configuration overrides.
    /// Use this dictionary to pass broker-specific settings that don't fit the generic model.
    /// Examples:
    /// - RabbitMQ: "Exchange", "RoutingKey", "ExchangeType"
    /// - Azure Service Bus: "SubscriptionName", "SessionEnabled"
    /// </summary>
    public Dictionary<string, string> ProviderSpecific { get; set; } = new();
}
