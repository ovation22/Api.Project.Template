namespace Api.Project.Template.Infrastructure.Messaging.Abstractions;

/// <summary>
/// Defines a message consumer that can subscribe to and process messages from a message broker.
/// </summary>
public interface IMessageConsumer : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Starts consuming messages from the message broker.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops consuming messages from the message broker.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
