using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Ardalis.Specification;
using System.Linq.Expressions;

namespace Api.Project.Template.Application.Abstractions;

public interface IRepository
{
    /// <summary>
    /// Gets a single entity type <typeparamref name="T"/> by its key.
    /// </summary>
    /// <typeparam name="T">The type of the entity to retrieve.</typeparam>
    /// <param name="id">The key of the entity to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains the entity of type <typeparamref name="T"/> if found; otherwise, null.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<T?> FindAsync<T>(object id, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Gets a single entity matching the given expression.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="expression">The expression to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<T?> SingleOrDefaultAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Gets a single entity matching the given specification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<T?> SingleOrDefaultAsync<T>(ISpecification<T> specification, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets a single entity matching the given specification and projects it to a result type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of projected results.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<TResult?> SingleOrDefaultAsync<T, TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets the first entity matching the given expression.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="expression">The expression to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Gets the first entity matching the given expression and projects it to a result type, applying an order by clause.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="expression">The expression to match against entities.</param>
    /// <param name="orderBy">The ordering function to apply to the query.</param>
    /// <param name="cancellationToken">The cancellation token to observe while the operation executes.</param>
    /// <returns>The first entity that matches the given expression and order, or null if no entity matches.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Gets the first entity matching the given specification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<T?> FirstOrDefaultAsync<T>(ISpecification<T> specification, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets all entities of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An enumerable of entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<IEnumerable<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lists entities matching the given expression.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="expression">The expression to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> expression,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lists entities matching the given specification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of entity results.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<List<T>> ListAsync<T>(ISpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lists entities matching the given specification and projects them to a result type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of projected results.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<List<TResult>> ListAsync<T, TResult>(ISpecification<T, TResult> specification,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Groups entities matching the given specification and projects them to a result type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TGrouping">The type of the grouping.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="groupBy">The grouping to apply.</param>
    /// <param name="selector">The selector to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of projected results.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<List<TResult>> ListAsync<T, TGrouping, TResult>(ISpecification<T> specification,
        Expression<Func<T, TGrouping>> groupBy,
        Expression<Func<IGrouping<TGrouping, T>, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lists paginated entities matching the given specification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<PagedList<T>> ListAsync<T>(PaginatedSpecification<T> specification,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Lists paginated entities matching the given specification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<PagedList<TResult>> ListAsync<T, TResult>(PaginatedSpecification<T, TResult> specification,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks whether any entities exist.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if there were any matching entities, otherwise false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks whether any entities match the given expression.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="expression">The expression to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if there were any matching entities, otherwise false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<bool> AnyAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks whether any entities match the given specification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if there were any matching entities, otherwise false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<bool> AnyAsync<T>(ISpecification<T> specification, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Counts the number of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of matching entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<int> CountAsync<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Counts the number of entities matching the given expression.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="expression">The expression to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of matching entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<int> CountAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Counts the number of entities matching the given specification.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="specification">The specification to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of matching entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<int> CountAsync<T>(ISpecification<T> specification, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Creates a new entity in the repository.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created entity.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="Exception">Thrown when a general error occurs while saving changes.</exception>
    Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Creates multiple new entities in the repository.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The entities to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="Exception">Thrown when a general error occurs while saving changes.</exception>
    Task<List<T>> CreateAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="Exception">Thrown when a general error occurs while saving changes.</exception>
    Task<T> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Updates multiple existing entities in the repository.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated entities.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="Exception">Thrown when a general error occurs while saving changes.</exception>
    Task<List<T>> UpdateAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Deletes multiple entities from the repository.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync<T>(List<T> entities, CancellationToken cancellationToken = default) where T : class;
}