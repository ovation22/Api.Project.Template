using Api.Project.Template.Application.Common.Pagination;
using Ardalis.Specification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Api.Project.Template.Application.Common.Specifications;

/// <summary>
/// Represents a paginated specification for entities.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="PaginatedSpecification{T}"/> class.
/// </remarks>
/// <param name="pageNumber">The page number.</param>
/// <param name="pageSize">The page size.</param>
public abstract class PaginatedSpecification<T>(int pageNumber, int pageSize) : Specification<T>, IPaginatedSpecification<T>
{
    /// <inheritdoc />
    public int PageNumber { get; } = pageNumber;

    /// <inheritdoc />
    public int PageSize { get; } = pageSize;

    /// <summary>
    /// Checks if the specified property exists in the entity type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The name of the property to check.</param>
    /// <returns>True if the property exists; otherwise, false.</returns>
    private static bool IsEntityProperty(string propertyName)
    {
        return GetPropertyExpression(propertyName) != null;
    }

    /// <summary>
    /// Gets the property expression for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The property expression if found; otherwise null.</returns>
    private static PropertyInfo? GetPropertyExpression(string propertyName)
    {
        var properties = propertyName.Split('.');
        var type = typeof(T);
        PropertyInfo? property = null;

        foreach (var prop in properties)
        {
            property = type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                return null;
            }

            type = property.PropertyType;
        }

        return property;
    }

    /// <summary>
    /// Builds the filter expression based on the filter operator and value.
    /// </summary>
    /// <param name="filterBy">The property name to filter by.</param>
    /// <param name="filter">The filter object containing the operator and value.</param>
    /// <param name="propertyMappings">The custom property mappings dictionary.</param>
    /// <returns>An expression representing the filter condition.</returns>
    private static Expression<Func<T, bool>> BuildFilterExpression(string filterBy, Filter filter, Dictionary<string, string> propertyMappings)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        // Map the filterBy to the actual property path if it exists in the dictionary
        if (propertyMappings.TryGetValue(filterBy, out var actualPropertyPath))
        {
            filterBy = actualPropertyPath;
        }

        Expression property = parameter;

        // Split the filterBy string into parts to handle nested properties
        foreach (var member in filterBy.Split('.'))
        {
            property = Expression.Property(property, member);
        }

        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type; // Handle nullable types
        Expression body;

        if (filter.Operator != FilterOperator.Between)
        {
            if (filter.Value == null)
            {
                throw new InvalidOperationException("Value is required for the selected operator.");
            }

            // Convert the constant to the appropriate type, including nullable if needed
            var constantValue = ConvertValue(filter.Value, propertyType);
            var constant = Expression.Constant(constantValue, propertyType); // Match constant type to property type

            // Coalesce property to zero if it’s nullable
            if (Nullable.GetUnderlyingType(property.Type) != null)
            {
                property = Expression.Coalesce(property, Expression.Constant(Convert.ChangeType(0, propertyType)));
            }

            body = filter.Operator switch
            {
                FilterOperator.Eq => Expression.Equal(property, constant),
                FilterOperator.Ne => Expression.NotEqual(property, constant),
                FilterOperator.Contains => Expression.Call(property, "Contains", null, constant),
                FilterOperator.StartsWith => Expression.Call(property, "StartsWith", null, constant),
                FilterOperator.EndsWith => Expression.Call(property, "EndsWith", null, constant),
                FilterOperator.Gt => Expression.GreaterThan(property, constant),
                FilterOperator.Gte => Expression.GreaterThanOrEqual(property, constant),
                FilterOperator.Lt => Expression.LessThan(property, constant),
                FilterOperator.Lte => Expression.LessThanOrEqual(property, constant),
                _ => throw new NotSupportedException($"Filter operator ‘{filter.Operator}’ is not supported.")
            };
        }
        else
        {
            if (filter.ValueFrom == null || filter.ValueTo == null)
            {
                throw new InvalidOperationException("ValueFrom and ValueTo are required for the selected operator.");
            }

            var fromConstantValue = ConvertValue(filter.ValueFrom, propertyType);
            var toConstantValue = ConvertValue(filter.ValueTo, propertyType);

            var fromConstant = Expression.Constant(fromConstantValue, propertyType);
            var toConstant = Expression.Constant(toConstantValue, propertyType);

            // Coalesce property to zero if it’s nullable
            if (Nullable.GetUnderlyingType(property.Type) != null)
            {
                property = Expression.Coalesce(property, Expression.Constant(Convert.ChangeType(0, propertyType)));
            }

            body = Expression.AndAlso(
                Expression.GreaterThanOrEqual(property, fromConstant),
                Expression.LessThanOrEqual(property, toConstant)
            );
        }

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Combines multiple filter expressions with an OR operator.
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
    /// <param name="filterExpressions">The filter expressions to combine.</param>
    /// <returns>The combined filter expression with an OR operator.</returns>
    private static Expression<Func<T, bool>> CombineFilterExpressionsWithOr(IEnumerable<Expression<Func<T, bool>>> filterExpressions)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        // Start with 'false' so OR chain works
        Expression combined = Expression.Constant(false);

        combined = filterExpressions
            .Select(expr => new ParameterReplacer(expr.Parameters[0], parameter).Visit(expr.Body)).Aggregate(combined,
                Expression.OrElse);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// ExpressionVisitor that replaces one parameter expression with another.
    /// </summary>
    private static object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(DateOnly))
            return DateOnly.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(TimeOnly))
            return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(Guid))
            return Guid.Parse(value);
        if (targetType == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        return Convert.ChangeType(value, targetType);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == from ? to : base.VisitParameter(node);
        }
    }

    /// <summary>
    /// Applies sorting logic based on entity properties.
    /// </summary>
    /// <param name="sortBy">The property to sort by.</param>
    /// <param name="sortDirection">The direction of the sort. If "DESC", sorting is in descending order; otherwise, sorting is in ascending order.</param>
    /// <remarks>
    /// This method creates an order by expression for the specified property and applies it to the query. 
    /// If the sort direction is "DESC", the query is ordered in descending order; otherwise, it is ordered in ascending order.
    /// </remarks>
    protected void ApplySorting(string sortBy, SortDirection sortDirection)
    {
        ApplySorting(sortBy, sortDirection, new Dictionary<string, string>());
    }

    /// <summary>
    /// Applies sorting logic based on entity properties.
    /// </summary>
    /// <param name="sortBy">The property to sort by.</param>
    /// <param name="sortDirection">The direction of the sort. If "DESC", sorting is in descending order; otherwise, sorting is in ascending order.</param>
    /// <param name="propertyMappings">The custom property mappings dictionary.</param>
    /// <remarks>
    /// This method creates an order by expression for the specified property and applies it to the query. 
    /// If the sort direction is "DESC", the query is ordered in descending order; otherwise, it is ordered in ascending order.
    /// </remarks>
    protected void ApplySorting(string sortBy, SortDirection sortDirection, Dictionary<string, string> propertyMappings)
    {
        if (propertyMappings.TryGetValue(sortBy, out var mappedProperty))
        {
            sortBy = mappedProperty;
        }

        if (IsEntityProperty(sortBy))
        {
            if (sortDirection == SortDirection.Desc)
            {
                Query.OrderByDescending(CreateOrderByExpression(sortBy)!);
            }
            else
            {
                Query.OrderBy(CreateOrderByExpression(sortBy)!);
            }
        }
        else
        {
            throw new NotSupportedException($"Order By property '{sortBy}' is not supported.");
        }
    }

    /// <summary>
    /// Creates an order by expression for the specified property in the entity type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyPath">The path of the property to order by, supporting dot notation for nested properties.</param>
    /// <returns>An expression representing the order by clause.</returns>
    protected static Expression<Func<T, object>> CreateOrderByExpression(string propertyPath)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression property = parameter;

        // Split the propertyPath into parts to handle nested properties (e.g., "Book.Grade")
        foreach (var member in propertyPath.Split('.'))
        {
            property = Expression.Property(property, member);
        }

        // Convert the property to type 'object' to match the return type
        var propertyAsObject = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object>>(propertyAsObject, parameter);
    }

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
        var filterList = filters.ToList();
        var filterExpressions = filterList.Select(filter =>
            PaginatedSpecification<T>.BuildFilterExpression(filter.Key, filter.Value, propertyMappings));

        var combinedFilterExpression = CombineFilterExpressionsWithOr(filterExpressions);
        Query.Where(combinedFilterExpression);
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
        foreach (var filterExpression in filters.Select(filter =>
                    PaginatedSpecification<T>.BuildFilterExpression(filter.Key, filter.Value, propertyMappings)))
        {
            Query.Where(filterExpression);
        }
    }
}

/// <summary>
/// Paginated specification that also projects to TResult.
/// Copy of helpers from the non-projected PaginatedSpecification to keep behavior consistent.
/// </summary>
public abstract class PaginatedSpecification<T, TResult>(int pageNumber, int pageSize)
    : Specification<T, TResult>, IPaginatedSpecification<T, TResult>
    where T : class
{
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;

    private static bool IsEntityProperty(string propertyName)
        => GetPropertyExpression(propertyName) != null;

    private static PropertyInfo? GetPropertyExpression(string propertyName)
    {
        var properties = propertyName.Split('.');
        var type = typeof(T);
        PropertyInfo? property = null;

        foreach (var prop in properties)
        {
            property = type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                return null;
            }

            type = property.PropertyType;
        }

        return property;
    }

    private static Expression<Func<T, bool>> BuildFilterExpression(string filterBy, Filter filter, Dictionary<string, string> propertyMappings)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        if (propertyMappings.TryGetValue(filterBy, out var actualPropertyPath))
        {
            filterBy = actualPropertyPath;
        }

        Expression property = filterBy.Split('.')
            .Aggregate<string?, Expression>(parameter, Expression.Property!);

        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        Expression body;

        if (filter.Operator != FilterOperator.Between)
        {
            if (filter.Value == null)
            {
                throw new InvalidOperationException("Value is required for the selected operator.");
            }

            var constantValue = ConvertValue(filter.Value, propertyType);
            var constant = Expression.Constant(constantValue, propertyType);

            if (Nullable.GetUnderlyingType(property.Type) != null)
            {
                property = Expression.Coalesce(property, Expression.Constant(Convert.ChangeType(0, propertyType)));
            }

            body = filter.Operator switch
            {
                FilterOperator.Eq => Expression.Equal(property, constant),
                FilterOperator.Ne => Expression.NotEqual(property, constant),
                FilterOperator.Contains => Expression.Call(property, "Contains", null, constant),
                FilterOperator.StartsWith => Expression.Call(property, "StartsWith", null, constant),
                FilterOperator.EndsWith => Expression.Call(property, "EndsWith", null, constant),
                FilterOperator.Gt => Expression.GreaterThan(property, constant),
                FilterOperator.Gte => Expression.GreaterThanOrEqual(property, constant),
                FilterOperator.Lt => Expression.LessThan(property, constant),
                FilterOperator.Lte => Expression.LessThanOrEqual(property, constant),
                _ => throw new NotSupportedException($"Filter operator '{filter.Operator}' is not supported.")
            };
        }
        else
        {
            if (filter.ValueFrom == null || filter.ValueTo == null)
            {
                throw new InvalidOperationException("ValueFrom and ValueTo are required for the selected operator.");
            }

            var fromConstantValue = ConvertValue(filter.ValueFrom, propertyType);
            var toConstantValue = ConvertValue(filter.ValueTo, propertyType);

            var fromConstant = Expression.Constant(fromConstantValue, propertyType);
            var toConstant = Expression.Constant(toConstantValue, propertyType);

            if (Nullable.GetUnderlyingType(property.Type) != null)
            {
                property = Expression.Coalesce(property, Expression.Constant(Convert.ChangeType(0, propertyType)));
            }

            body = Expression.AndAlso(
                Expression.GreaterThanOrEqual(property, fromConstant),
                Expression.LessThanOrEqual(property, toConstant)
            );
        }

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(DateOnly))
            return DateOnly.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(TimeOnly))
            return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(Guid))
            return Guid.Parse(value);
        if (targetType == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        return Convert.ChangeType(value, targetType);
    }

    private static Expression<Func<T, object>> CreateOrderByExpression(string propertyPath)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression property = propertyPath.Split('.')
            .Aggregate<string?, Expression>(parameter, Expression.Property!);
        var propertyAsObject = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object>>(propertyAsObject, parameter);
    }

    protected void ApplySorting(string sortBy, SortDirection sortDirection, Dictionary<string, string> propertyMappings)
    {
        if (propertyMappings.TryGetValue(sortBy, out var mapped))
        {
            sortBy = mapped;
        }

        if (IsEntityProperty(sortBy))
        {
            if (sortDirection == SortDirection.Desc)
            {
                Query.OrderByDescending(CreateOrderByExpression(sortBy)!);
            }
            else
            {
                Query.OrderBy(CreateOrderByExpression(sortBy)!);
            }
        }
        else
        {
            throw new NotSupportedException($"Order By property '{sortBy}' is not supported.");
        }
    }

    private static Expression<Func<T, bool>> CombineFilterExpressionsWithOr(IEnumerable<Expression<Func<T, bool>>> filterExpressions)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression combined = Expression.Constant(false);
        combined = filterExpressions
            .Select(expr => new ParameterReplacer(expr.Parameters[0], parameter).Visit(expr.Body)!).Aggregate(combined,
                Expression.OrElse);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    protected void ApplyOrFilters(Dictionary<string, Filter> filters, Dictionary<string, string> propertyMappings)
    {
        var filterList = filters.ToList();
        var filterExpressions = filterList.Select(filter => BuildFilterExpression(filter.Key, filter.Value, propertyMappings));
        var combinedFilterExpression = CombineFilterExpressionsWithOr(filterExpressions);
        Query.Where(combinedFilterExpression);
    }

    protected void ApplyAndFilters(Dictionary<string, Filter> filters, Dictionary<string, string> propertyMappings)
    {
        foreach (var filterExpression in filters.Select(filter => BuildFilterExpression(filter.Key, filter.Value, propertyMappings)))
            Query.Where(filterExpression);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}