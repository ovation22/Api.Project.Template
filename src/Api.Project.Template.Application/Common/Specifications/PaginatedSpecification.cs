using Api.Project.Template.Application.Common.Pagination;
using Ardalis.Specification;
using System.Linq.Expressions;

namespace Api.Project.Template.Application.Common.Specifications;

/// <summary>
/// Represents a paginated specification for entities.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
/// <param name="pageNumber">The page number.</param>
/// <param name="pageSize">The page size.</param>
/// <remarks>
/// Sorting and filtering on client-supplied property names throws <see cref="InvalidSpecificationException"/>
/// for unknown or computed properties, unconvertible values, and operators that don't fit the property type.
/// </remarks>
public abstract class PaginatedSpecification<T>(int pageNumber, int pageSize) : Specification<T>, IPaginatedSpecification<T>
{
    /// <inheritdoc />
    public int PageNumber { get; } = pageNumber;

    /// <inheritdoc />
    public int PageSize { get; } = pageSize;

    /// <summary>
    /// Applies sorting logic based on entity properties.
    /// </summary>
    /// <param name="sortBy">The property to sort by.</param>
    /// <param name="sortDirection">The direction of the sort.</param>
    protected void ApplySorting(string sortBy, SortDirection sortDirection)
    {
        ApplySorting(sortBy, sortDirection, new Dictionary<string, string>());
    }

    /// <summary>
    /// Applies sorting logic based on entity properties.
    /// </summary>
    /// <param name="sortBy">The property to sort by.</param>
    /// <param name="sortDirection">The direction of the sort.</param>
    /// <param name="propertyMappings">The custom property mappings dictionary.</param>
    /// <remarks>
    /// When the entity has an <c>Id</c> property, it is added as a secondary sort so that
    /// pages are stable when the sort property has duplicate values.
    /// </remarks>
    protected void ApplySorting(string sortBy, SortDirection sortDirection, Dictionary<string, string> propertyMappings)
    {
        var orderBy = SpecificationExpressions.CreateOrderBy<T>(sortBy, propertyMappings);
        var ordered = sortDirection == SortDirection.Desc
            ? Query.OrderByDescending(orderBy!)
            : Query.OrderBy(orderBy!);

        var tieBreaker = SpecificationExpressions.CreateTieBreaker<T>(sortBy, propertyMappings);
        if (tieBreaker != null)
        {
            ordered.ThenBy(tieBreaker!);
        }
    }

    /// <summary>
    /// Creates an order by expression for the specified property in the entity type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyPath">The path of the property to order by, supporting dot notation for nested properties.</param>
    /// <returns>An expression representing the order by clause.</returns>
    protected static Expression<Func<T, object>> CreateOrderByExpression(string propertyPath)
        => SpecificationExpressions.CreateOrderBy<T>(propertyPath, []);

    /// <summary>
    /// Applies filters with an OR logic.
    /// </summary>
    /// <param name="filters">The dictionary of filters to apply.</param>
    protected void ApplyOrFilters(Dictionary<string, Filter> filters)
    {
        ApplyOrFilters(filters, new Dictionary<string, string>());
    }

    /// <summary>
    /// Applies filters with an OR logic.
    /// </summary>
    /// <param name="filters">The dictionary of filters to apply.</param>
    /// <param name="propertyMappings">The custom property mappings dictionary.</param>
    protected void ApplyOrFilters(Dictionary<string, Filter> filters, Dictionary<string, string> propertyMappings)
    {
        var filterExpressions = filters
            .Select(filter => SpecificationExpressions.CreateFilter<T>(filter.Key, filter.Value, propertyMappings))
            .ToList();

        Query.Where(SpecificationExpressions.CombineWithOr(filterExpressions));
    }

    /// <summary>
    /// Applies filters with an AND logic.
    /// </summary>
    /// <param name="filters">The dictionary of filters to apply.</param>
    protected void ApplyAndFilters(Dictionary<string, Filter> filters)
    {
        ApplyAndFilters(filters, []);
    }

    /// <summary>
    /// Applies filters with an AND logic.
    /// </summary>
    /// <param name="filters">The dictionary of filters to apply.</param>
    /// <param name="propertyMappings">The custom property mappings dictionary.</param>
    protected void ApplyAndFilters(Dictionary<string, Filter> filters, Dictionary<string, string> propertyMappings)
    {
        foreach (var filter in filters)
        {
            Query.Where(SpecificationExpressions.CreateFilter<T>(filter.Key, filter.Value, propertyMappings));
        }
    }
}

/// <summary>
/// Represents a paginated specification for entities that projects results to <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
/// <typeparam name="TResult">The type of the projected result.</typeparam>
/// <param name="pageNumber">The page number.</param>
/// <param name="pageSize">The page size.</param>
public abstract class PaginatedSpecification<T, TResult>(int pageNumber, int pageSize)
    : Specification<T, TResult>, IPaginatedSpecification<T, TResult>
    where T : class
{
    /// <inheritdoc />
    public int PageNumber { get; } = pageNumber;

    /// <inheritdoc />
    public int PageSize { get; } = pageSize;

    /// <inheritdoc cref="PaginatedSpecification{T}.ApplySorting(string, SortDirection, Dictionary{string,string})"/>
    protected void ApplySorting(string sortBy, SortDirection sortDirection, Dictionary<string, string> propertyMappings)
    {
        var orderBy = SpecificationExpressions.CreateOrderBy<T>(sortBy, propertyMappings);
        var ordered = sortDirection == SortDirection.Desc
            ? Query.OrderByDescending(orderBy!)
            : Query.OrderBy(orderBy!);

        var tieBreaker = SpecificationExpressions.CreateTieBreaker<T>(sortBy, propertyMappings);
        if (tieBreaker != null)
        {
            ordered.ThenBy(tieBreaker!);
        }
    }

    /// <inheritdoc cref="PaginatedSpecification{T}.ApplyOrFilters(Dictionary{string,Filter}, Dictionary{string,string})"/>
    protected void ApplyOrFilters(Dictionary<string, Filter> filters, Dictionary<string, string> propertyMappings)
    {
        var filterExpressions = filters
            .Select(filter => SpecificationExpressions.CreateFilter<T>(filter.Key, filter.Value, propertyMappings))
            .ToList();

        Query.Where(SpecificationExpressions.CombineWithOr(filterExpressions));
    }

    /// <inheritdoc cref="PaginatedSpecification{T}.ApplyAndFilters(Dictionary{string,Filter}, Dictionary{string,string})"/>
    protected void ApplyAndFilters(Dictionary<string, Filter> filters, Dictionary<string, string> propertyMappings)
    {
        foreach (var filter in filters)
        {
            Query.Where(SpecificationExpressions.CreateFilter<T>(filter.Key, filter.Value, propertyMappings));
        }
    }
}
