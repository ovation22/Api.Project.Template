using Api.Project.Template.Application.Common.Pagination;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Api.Project.Template.Application.Common.Specifications;

/// <summary>
/// Builds the sort and filter expressions shared by <see cref="PaginatedSpecification{T}"/>
/// and <see cref="PaginatedSpecification{T, TResult}"/>.
/// Invalid client input is reported as <see cref="InvalidSpecificationException"/>.
/// </summary>
internal static class SpecificationExpressions
{
    private const string TieBreakerProperty = "Id";

    /// <summary>
    /// Creates an order-by expression for the (mapped) property path.
    /// </summary>
    /// <param name="sortBy">The property name as supplied by the caller.</param>
    /// <param name="propertyMappings">Custom property mappings (caller name → property path).</param>
    public static Expression<Func<T, object>> CreateOrderBy<T>(string sortBy, Dictionary<string, string> propertyMappings)
    {
        var propertyPath = propertyMappings.GetValueOrDefault(sortBy, sortBy);
        ResolveProperty<T>(propertyPath, sortBy, "Sorting");

        return CreateOrderByExpression<T>(propertyPath);
    }

    /// <summary>
    /// Returns an order-by expression on <c>Id</c> to use as a tie-breaker, so paging is stable
    /// when the sort property isn't unique. Returns null when <typeparamref name="T"/> has no
    /// <c>Id</c> property or the sort is already on it.
    /// </summary>
    public static Expression<Func<T, object>>? CreateTieBreaker<T>(string sortBy, Dictionary<string, string> propertyMappings)
    {
        var propertyPath = propertyMappings.GetValueOrDefault(sortBy, sortBy);
        var idProperty = FindProperty<T>(TieBreakerProperty);

        if (idProperty == null || string.Equals(propertyPath, idProperty.Name, StringComparison.OrdinalIgnoreCase))
            return null;

        return CreateOrderByExpression<T>(idProperty.Name);
    }

    /// <summary>
    /// Builds the filter expression for a single filter.
    /// </summary>
    /// <param name="filterBy">The property name as supplied by the caller.</param>
    /// <param name="filter">The filter operator and value(s).</param>
    /// <param name="propertyMappings">Custom property mappings (caller name → property path).</param>
    public static Expression<Func<T, bool>> CreateFilter<T>(string filterBy, Filter filter, Dictionary<string, string> propertyMappings)
    {
        var propertyPath = propertyMappings.GetValueOrDefault(filterBy, filterBy);
        ResolveProperty<T>(propertyPath, filterBy, "Filtering");

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = CreatePropertyAccess(parameter, propertyPath);
        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

        // Nullable properties are compared as their default value (e.g. null → 0 for numbers)
        if (Nullable.GetUnderlyingType(property.Type) != null)
        {
            property = Expression.Coalesce(property, Expression.Default(propertyType));
        }

        Expression body;
        try
        {
            if (filter.Operator != FilterOperator.Between)
            {
                if (filter.Value == null)
                {
                    throw new InvalidSpecificationException(filterBy, $"A value is required to filter '{filterBy}' with '{filter.Operator}'.");
                }

                var constant = Expression.Constant(ConvertValue(filter.Value, propertyType, filterBy), propertyType);

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
                    _ => throw new InvalidSpecificationException(filterBy, $"Filter operator '{filter.Operator}' is not supported.")
                };
            }
            else
            {
                if (filter.ValueFrom == null || filter.ValueTo == null)
                {
                    throw new InvalidSpecificationException(filterBy, $"ValueFrom and ValueTo are required to filter '{filterBy}' with 'Between'.");
                }

                var fromConstant = Expression.Constant(ConvertValue(filter.ValueFrom, propertyType, filterBy), propertyType);
                var toConstant = Expression.Constant(ConvertValue(filter.ValueTo, propertyType, filterBy), propertyType);

                body = Expression.AndAlso(
                    Expression.GreaterThanOrEqual(property, fromConstant),
                    Expression.LessThanOrEqual(property, toConstant));
            }
        }
        catch (InvalidOperationException)
        {
            // Expression.GreaterThan / Expression.Call throw when the operator isn't defined for
            // the property type (e.g. Gt on a string, Contains on a number).
            throw new InvalidSpecificationException(filterBy, $"Filter operator '{filter.Operator}' cannot be used with '{filterBy}'.");
        }

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Combines filter expressions with OR.
    /// </summary>
    public static Expression<Func<T, bool>> CombineWithOr<T>(IEnumerable<Expression<Func<T, bool>>> filterExpressions)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        // Start with 'false' so the OR chain works
        var combined = filterExpressions
            .Select(expr => new ParameterReplacer(expr.Parameters[0], parameter).Visit(expr.Body))
            .Aggregate((Expression)Expression.Constant(false), Expression.OrElse);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// Ensures the property path exists and is settable. Get-only properties (e.g. computed
    /// <c>TemperatureF</c>) are rejected because EF can't translate them to SQL.
    /// </summary>
    private static void ResolveProperty<T>(string propertyPath, string requestedName, string operation)
    {
        var property = FindProperty<T>(propertyPath);

        if (property == null || !property.CanWrite)
        {
            throw new InvalidSpecificationException(requestedName, $"{operation} by '{requestedName}' is not supported.");
        }
    }

    private static PropertyInfo? FindProperty<T>(string propertyPath)
    {
        var type = typeof(T);
        PropertyInfo? property = null;

        foreach (var name in propertyPath.Split('.'))
        {
            property = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                return null;
            }

            type = property.PropertyType;
        }

        return property;
    }

    private static Expression CreatePropertyAccess(ParameterExpression parameter, string propertyPath)
        => propertyPath.Split('.').Aggregate<string, Expression>(parameter, Expression.Property);

    private static Expression<Func<T, object>> CreateOrderByExpression<T>(string propertyPath)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = CreatePropertyAccess(parameter, propertyPath);

        return Expression.Lambda<Func<T, object>>(Expression.Convert(property, typeof(object)), parameter);
    }

    private static object ConvertValue(string value, Type targetType, string filterBy)
    {
        try
        {
            if (targetType == typeof(DateOnly))
                return DateOnly.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(TimeOnly))
                return TimeOnly.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(Guid))
                return Guid.Parse(value);
            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new InvalidSpecificationException(filterBy, $"'{value}' is not a valid value for '{filterBy}'.");
        }
    }

    /// <summary>
    /// Replaces one parameter expression with another within an expression tree.
    /// </summary>
    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
