using Api.Project.Template.Application.Abstractions.Data;
using Api.Project.Template.Application.Abstractions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Project.Template.Infrastructure.Data;

public class TransactionManager(ApiProjectTemplateContext dbContext, ILoggerAdapter<TransactionManager> logger) : ITransactionManager
{
    private readonly ApiProjectTemplateContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ILoggerAdapter<TransactionManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private IDbContextTransaction? _currentTransaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already active. Nested transactions are not supported.");
        }

        // A retrying execution strategy (enabled by Aspire's AddSqlServerDbContext/AddNpgsqlDbContext)
        // rejects user-initiated transactions; fail here with guidance instead of on the first SaveChanges.
        if (_dbContext.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            throw new InvalidOperationException(
                "BeginTransactionAsync can't be used with a retrying execution strategy. " +
                "Use ExecuteAsync, which runs the transaction under the strategy so it can be retried as a unit.");
        }

        _logger.LogDebug("Beginning database transaction");
        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            _logger.LogDebug("Committing database transaction");
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback.");
        }

        try
        {
            _logger.LogWarning("Rolling back database transaction");
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        // Run the whole transaction under the execution strategy so a transient failure retries it as a unit.
        // (A retrying strategy rejects transactions opened outside of it.)
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                _logger.LogDebug("Executing operation within transaction");
                var result = await operation();

                await transaction.CommitAsync(ct);
                _logger.LogDebug("Transaction committed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation failed, rolling back transaction");
                try
                {
                    await transaction.RollbackAsync(ct);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback transaction after operation failure");
                }
                throw;
            }
        }, cancellationToken);
    }
}
