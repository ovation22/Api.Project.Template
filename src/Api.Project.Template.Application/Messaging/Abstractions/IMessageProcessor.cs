namespace Api.Project.Template.Application.Messaging.Abstractions;

/// <summary>
/// Generic message processor interface.
/// Implementations handle processing logic for a specific message type.
/// </summary>
/// <typeparam name="TMessage">The type of message this processor handles.</typeparam>
public interface IMessageProcessor<in TMessage>
{
    /// <summary>
    /// Processes a message and returns the result.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="context">
    /// Message context containing metadata like correlation ID, delivery count, and cancellation token.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous processing operation.
    /// The task result indicates success or failure and whether to requeue on failure.
    /// </returns>
    /// <remarks>
    /// Implementation guidelines:
    /// - Use context.CancellationToken to check for cancellation
    /// - Use context.CorrelationId for distributed tracing
    /// - Return MessageProcessingResult.Succeeded() on success
    /// - Return MessageProcessingResult.Failed(reason, requeue: true) for transient errors (network, database timeout)
    /// - Return MessageProcessingResult.Failed(reason, requeue: false) for permanent errors (validation, business logic)
    /// - Catch and handle expected exceptions, let unexpected exceptions bubble up
    /// </remarks>
    Task<MessageProcessingResult> ProcessAsync(TMessage message, MessageContext context);
}
