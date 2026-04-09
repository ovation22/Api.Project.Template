using Api.Project.Template.Application.Messaging;

namespace Api.Project.Template.Infrastructure.Messaging.Abstractions;

/// <summary>
/// Adapter interface for message broker operations.
/// Implementations wrap broker-specific APIs (RabbitMQ, Azure Service Bus, etc.)
/// to provide a consistent interface for consuming messages.
/// </summary>
public interface IMessageBrokerAdapter : IAsyncDisposable
{
    /// <summary>
    /// Configures the adapter and, where possible, establishes the broker connection.
    /// </summary>
    /// <param name="config">The broker configuration including connection string, queue name, and concurrency settings.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid or missing required values.</exception>
    /// <remarks>
    /// Implementations are expected to store <paramref name="config"/> and prepare any resources needed
    /// before <see cref="SubscribeAsync{TMessage}"/> is called.
    /// <para>
    /// Eager connection (RabbitMQ): the physical TCP connection and channel are opened here.
    /// </para>
    /// <para>
    /// Deferred connection (Azure Service Bus): the SDK processor is configured here but the
    /// underlying connection is not opened until <see cref="SubscribeAsync{TMessage}"/> calls
    /// <c>StartProcessingAsync</c>. This is a deliberate SDK design choice — there is no
    /// connect-only API on <c>ServiceBusProcessor</c>.
    /// </para>
    /// </remarks>
    Task ConnectAsync(MessageBrokerConfig config, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to messages of type TMessage and routes them to the provided handler.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to consume (will be deserialized from JSON).</typeparam>
    /// <param name="handler">
    /// The handler function to process received messages.
    /// Receives the deserialized message and metadata context, returns processing result.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to stop consuming messages.</param>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    /// <remarks>
    /// This method should:
    /// - Start consuming messages from the configured queue
    /// - Deserialize message payload to TMessage
    /// - Create MessageContext with metadata (correlation ID, delivery count, etc.)
    /// - Invoke the handler and await the result
    /// - Based on result: ACK (success), NACK with requeue (transient failure), or dead-letter (permanent failure)
    /// - Respect concurrency limits (max concurrent handlers)
    /// </remarks>
    Task SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, Task<MessageProcessingResult>> handler,
        CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the message broker and cleans up resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    /// <remarks>
    /// This method should:
    /// - Stop consuming new messages
    /// - Allow in-flight messages to complete (graceful shutdown)
    /// - Close broker connections
    /// - Release resources (channels, connections, semaphores)
    /// </remarks>
    Task DisconnectAsync();
}
