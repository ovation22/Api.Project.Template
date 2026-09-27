namespace Api.Project.Template.Application.Abstractions.Data;

/// <summary>
/// Manages database transaction lifecycle, coordinating multiple repository operations
/// as a single atomic transaction.
/// </summary>
/// <remarks>
/// This interface provides explicit transaction management,
/// separating transaction control from repository data access operations.
/// Repository methods continue to save changes immediately; this manager
/// provides transaction boundaries around groups of repository operations.
/// </remarks>
public interface ITransactionManager
{
    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a transaction is already active, or if the database is configured to retry on
    /// transient failures (the Aspire default) — a manually opened transaction can't be retried
    /// as a unit, so use <see cref="ExecuteAsync(Func{Task}, CancellationToken)"/> instead.
    /// </exception>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction, persisting all changes made within the transaction scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if no transaction is active.</exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction, discarding all changes made within the transaction scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if no transaction is active.</exception>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the provided asynchronous operation within a database transaction.
    /// Automatically commits on success or rolls back on exception.
    /// </summary>
    /// <remarks>
    /// Runs under the database's execution strategy: on a transient failure the transaction is
    /// rolled back and the whole operation runs again, so it must be safe to repeat.
    /// </remarks>
    /// <param name="operation">The async operation to execute inside the transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown if operation is null.</exception>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the provided asynchronous operation within a database transaction and returns a result.
    /// Automatically commits on success or rolls back on exception.
    /// </summary>
    /// <remarks>
    /// Runs under the database's execution strategy: on a transient failure the transaction is
    /// rolled back and the whole operation runs again, so it must be safe to repeat.
    /// </remarks>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="operation">The async operation to execute inside the transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if operation is null.</exception>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
