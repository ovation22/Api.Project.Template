using Api.Project.Template.Application.Abstractions;
using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Api.Project.Template.Infrastructure.Data.Repositories;

/// <summary>
/// Abstract Repository class providing basic CRUD operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EFRepository"/> class.
/// </remarks>
/// <param name="dbContext">The database context.</param>
public abstract class EFRepository(DbContext dbContext, ILoggerAdapter<EFRepository> logger)
    : IRepository
{
    /// <inheritdoc />
    public virtual async Task<T?> FindAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id), "Identifier cannot be null");
        }

        return await dbContext.Set<T>().FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
        where T : class
    {
        return await dbContext.Set<T>()
            .SingleOrDefaultAsync(expression, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> SingleOrDefaultAsync<T>(ISpecification<T> specification, CancellationToken cancellationToken = default) where T : class
    {
        var specificationResult = ApplySpecification(specification);

        return await specificationResult
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TResult?> SingleOrDefaultAsync<T, TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default) where T : class
    {
        var specificationResult = ApplySpecification(specification);

        return await specificationResult
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
        where T : class
    {
        return await dbContext.Set<T>()
            .FirstOrDefaultAsync(expression, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy, CancellationToken cancellationToken = default)
        where T : class
    {
        var query = dbContext.Set<T>()
            .Where(expression);

        query = orderBy(query);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultAsync<T>(ISpecification<T> specification, CancellationToken cancellationToken = default) where T : class
    {
        var specificationResult = ApplySpecification(specification);

        return await specificationResult
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return await dbContext.Set<T>()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<T> AsAsyncEnumerable<T>(ISpecification<T> specification) where T : class
    {
        var query = ApplySpecification(specification);

        return query.AsAsyncEnumerable();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<T> AsAsyncEnumerable<T>(Expression<Func<T, bool>> expression) where T : class
    {
        return dbContext.Set<T>().AsQueryable().Where(expression).AsAsyncEnumerable();
    }

    /// <inheritdoc />
    public IEnumerable<T> AsEnumerable<T>(ISpecification<T> specification) where T : class
    {
        var query = ApplySpecification(specification);

        return query.AsEnumerable();
    }

    /// <inheritdoc />
    public IEnumerable<T> AsEnumerable<T>(Expression<Func<T, bool>> expression) where T : class
    {
        return dbContext.Set<T>().AsQueryable().Where(expression).AsEnumerable();
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> expression,
        CancellationToken cancellationToken = default) where T : class
    {
        return await dbContext.Set<T>()
            .Where(expression)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> ListAsync<T>(ISpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(specification);

        return await query
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<TResult>> ListAsync<T, TResult>(ISpecification<T, TResult> specification,
        CancellationToken cancellationToken = default) where T : class
    {
        if (specification.Selector == null)
        {
            return [];
        }

        var query = ApplySpecification(specification);

        return await query
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<TResult>> ListAsync<T, TGrouping, TResult>(ISpecification<T> specification,
        Expression<Func<T, TGrouping>> groupBy,
        Expression<Func<IGrouping<TGrouping, T>, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(specification);

        return await query
            .GroupBy(groupBy)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PagedList<T>> ListAsync<T>(PaginatedSpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class
    {
        var countQuery = ApplySpecification(specification);
        var totalCount = await countQuery.CountAsync(cancellationToken);

        var query = ApplySpecification(specification);
        var items = await query
            .Skip((specification.PageNumber - 1) * specification.PageSize)
            .Take(specification.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, totalCount, specification.PageNumber, specification.PageSize);
    }

    /// <inheritdoc />
    public virtual async Task<PagedList<TResult>> ListAsync<T, TResult>(PaginatedSpecification<T, TResult> specification,
        CancellationToken cancellationToken = default) where T : class
    {
        var countQuery = ApplySpecification(specification);
        var totalCount = await countQuery.CountAsync(cancellationToken);

        var query = ApplySpecification(specification);
        var items = await query
            .Skip((specification.PageNumber - 1) * specification.PageSize)
            .Take(specification.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<TResult>(items, totalCount, specification.PageNumber, specification.PageSize);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return await dbContext.Set<T>()
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> expression,
        CancellationToken cancellationToken = default) where T : class
    {
        return await dbContext.Set<T>()
            .Where(expression)
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync<T>(ISpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(specification);

        return await query
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return await dbContext.Set<T>()
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync<T>(Expression<Func<T, bool>> expression,
        CancellationToken cancellationToken = default) where T : class
    {
        return await dbContext.Set<T>()
            .Where(expression)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync<T>(ISpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = ApplySpecification(specification);

        return await query
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        var dbSet = dbContext.Set<T>();
        dbSet.Add(entity);
        await SaveChangesAsync(cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> CreateAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        var dbSet = dbContext.Set<T>();
        dbSet.AddRange(entities);
        await SaveChangesAsync(cancellationToken);

        return entities;
    }

    /// <inheritdoc />
    public virtual async Task<T> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        var dbSet = dbContext.Set<T>();
        dbSet.Update(entity);
        await SaveChangesAsync(cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> UpdateAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        var dbSet = dbContext.Set<T>();
        dbSet.UpdateRange(entities);
        await SaveChangesAsync(cancellationToken);

        return entities;
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        var dbSet = dbContext.Set<T>();
        dbSet.Remove(entity);

        await SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        var dbSet = dbContext.Set<T>();
        dbSet.RemoveRange(entities);

        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves the changes asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency issues
            logger.LogError(ex, "A concurrency issue occurred.");
            throw new Exception("A concurrency issue occurred.", ex);
        }
        catch (DbUpdateException ex)
        {
            // Handle other update issues
            logger.LogError(ex, "An error occurred while updating the database.");
            throw new Exception("An error occurred while updating the database.", ex);
        }
        catch (TaskCanceledException ex)
        {
            // Handle task cancellation
            logger.LogWarning(ex, "The operation was canceled.");
            throw new OperationCanceledException("The operation was canceled.", ex, cancellationToken);
        }
        catch (Exception ex)
        {
            // Handle any other issues
            logger.LogError(ex, "An error occurred while saving changes.");
            throw new Exception("An error occurred while saving changes.", ex);
        }
    }

    /// <summary>
    /// Applies the given specification to the query and returns the queryable result.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="specification">The specification that defines the criteria and includes for the query.</param>
    /// <returns>An <see cref="IQueryable{T}"/> that represents the queryable result after applying the specification.</returns>
    protected IQueryable<T> ApplySpecification<T>(ISpecification<T> specification) where T : class
    {
        var evaluator = SpecificationEvaluator.Default;

        return evaluator.GetQuery(dbContext.Set<T>().TagWith(specification.GetType().Name), specification);
    }

    /// <summary>
    /// Applies the specification to the query for the specified entity type and projects the result to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="specification">The specification that defines the criteria for querying entities of type <typeparamref name="T"/> and projects the result to <typeparamref name="TResult"/>.</param>
    /// <returns>An <see cref="IQueryable{TResult}"/> representing the queryable result set projected to <typeparamref name="TResult"/> matching the specification.</returns>
    protected IQueryable<TResult> ApplySpecification<T, TResult>(ISpecification<T, TResult> specification) where T : class
    {
        var evaluator = SpecificationEvaluator.Default;

        return evaluator.GetQuery(dbContext.Set<T>().TagWith(specification.GetType().Name), specification);
    }
}