namespace Api.Project.Template.Application.Messaging;

/// <summary>
/// Message metadata and control context passed to message handlers.
/// Contains all contextual information about a received message.
/// </summary>
public class MessageContext
{
    /// <summary>
    /// Unique message identifier assigned by the message broker.
    /// </summary>
    public string MessageId { get; init; } = string.Empty;

    /// <summary>
    /// Correlation ID for distributed tracing and request tracking.
    /// Used to correlate related messages across different services.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Number of times this message has been delivered.
    /// Starts at 1 for first delivery, increments on each retry.
    /// </summary>
    public int DeliveryCount { get; init; }

    /// <summary>
    /// Message headers and properties.
    /// Contains metadata like content type, timestamp, custom headers, etc.
    /// </summary>
    public IReadOnlyDictionary<string, object> Headers { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Cancellation token for processing timeout and graceful shutdown.
    /// Processors should check this token and cancel long-running operations when requested.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Provider-specific metadata for advanced scenarios.
    /// Contains broker-specific objects like delivery tags, partition keys, etc.
    /// This is internal and used by adapters to pass provider-specific context.
    /// </summary>
    internal object? ProviderContext { get; init; }
}
