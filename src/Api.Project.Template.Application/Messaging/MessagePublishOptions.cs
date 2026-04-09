namespace Api.Project.Template.Application.Messaging;

/// <summary>
/// Options for publishing a message. Provider-agnostic wrapper for routing and delivery options.
/// </summary>
public class MessagePublishOptions
{
    /// <summary>
    /// The destination topic/queue/subject for the message.
    /// Maps to: RabbitMQ exchange, Azure Service Bus topic, AWS SNS topic, etc.
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// The routing key or subject for the message.
    /// Maps to: RabbitMQ routing key, Azure Service Bus subject, AWS SNS message attributes, etc.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Additional metadata for the message.
    /// Maps to provider-specific headers/properties.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }
}
