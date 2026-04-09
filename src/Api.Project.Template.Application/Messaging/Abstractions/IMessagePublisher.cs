namespace Api.Project.Template.Application.Messaging.Abstractions;

/// <summary>
/// Defines a message publisher that can send messages to a message broker.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message with provider-specific routing options.
    /// </summary>
    /// <param name="message">The message to publish</param>
    /// <param name="options">Optional routing and delivery options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T message, MessagePublishOptions? options = null, CancellationToken cancellationToken = default);
}
