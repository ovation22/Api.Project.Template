namespace Api.Project.Template.Application.Messaging;

/// <summary>
/// Result of message processing, determines ACK/NACK/Dead-letter behavior.
/// </summary>
public class MessageProcessingResult
{
    /// <summary>
    /// Indicates whether the message was processed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Indicates whether the message should be requeued for retry if processing failed.
    /// Only applicable when Success is false.
    /// </summary>
    public bool Requeue { get; init; }

    /// <summary>
    /// Human-readable error reason if processing failed.
    /// </summary>
    public string? ErrorReason { get; init; }

    /// <summary>
    /// Exception that caused the processing failure, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful processing result.
    /// The message will be acknowledged (ACKed) and removed from the queue.
    /// </summary>
    /// <returns>A MessageProcessingResult indicating success.</returns>
    public static MessageProcessingResult Succeeded() =>
        new() { Success = true };

    /// <summary>
    /// Creates a failed processing result.
    /// The message will be negatively acknowledged (NACKed).
    /// </summary>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="requeue">
    /// If true, the message will be requeued for retry.
    /// If false, the message will be dead-lettered (after max retries).
    /// </param>
    /// <returns>A MessageProcessingResult indicating failure.</returns>
    public static MessageProcessingResult Failed(string reason, bool requeue = false) =>
        new() { Success = false, Requeue = requeue, ErrorReason = reason };

    /// <summary>
    /// Creates a failed processing result with an exception.
    /// The message will be negatively acknowledged (NACKed).
    /// </summary>
    /// <param name="ex">The exception that caused the failure.</param>
    /// <param name="requeue">
    /// If true, the message will be requeued for retry.
    /// If false, the message will be dead-lettered (after max retries).
    /// </param>
    /// <returns>A MessageProcessingResult indicating failure with exception details.</returns>
    public static MessageProcessingResult FailedWithException(Exception ex, bool requeue = false) =>
        new() { Success = false, Requeue = requeue, Exception = ex, ErrorReason = ex.Message };
}
