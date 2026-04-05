using Api.Project.Template.Application.Validators;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Api.Project.Template.Application.Common.Pagination;

/// <summary>
/// Represents a pagination request for querying data with optional filtering and sorting.
/// </summary>
public record PaginationRequest
{
    /// <summary>
    /// Gets or initializes the page number for pagination.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0.")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets or initializes the page size for pagination.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page size must be greater than 0.")]
    public int Size { get; init; } = 10;

    /// <summary>
    /// Gets or initializes the property name to sort by.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets or initializes the sort direction. Default is ascending.
    /// </summary>
    public SortDirection Direction { get; init; } = SortDirection.Asc;

    /// <summary>
    /// Gets or initializes the logical operator to combine filters (AND/OR).
    /// </summary>
    public LogicalOperator Operator { get; init; } = LogicalOperator.Or;

    /// <summary>
    /// Gets or initializes the dictionary of filters to apply.
    /// </summary>
    /// <example>{ "Filters[Summary].Operator": "Eq", "Filters[Summary].Value": "Balmy" }</example>
    public Dictionary<string, Filter>? Filters { get; init; }
}

/// <summary>
/// Specifies the logical operators for combining filters.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// Specifies the logical AND operator.
    /// </summary>
    And,

    /// <summary>
    /// Specifies the logical OR operator.
    /// </summary>
    Or
}

/// <summary>
/// Specifies the sort directions for ordering.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Specifies ascending sort order.
    /// </summary>
    Asc,

    /// <summary>
    /// Specifies descending sort order.
    /// </summary>
    Desc
}

/// <summary>
/// Represents a filter condition to be applied in a query.
/// </summary>
public record Filter
{
    /// <summary>
    /// Gets or initializes the operator for the filter condition.
    /// </summary>
    public FilterOperator Operator { get; init; }

    /// <summary>
    /// Gets or initializes the value to filter by.
    /// </summary>
    [RequiredIfNot(nameof(Operator), FilterOperator.Between)]
    public string? Value { get; init; } = null!;

    /// <summary>
    /// Gets or initializes the range values for between filter.
    /// </summary>
    [RequiredIf(nameof(Operator), FilterOperator.Between)]
    public string? ValueFrom { get; init; }

    /// <summary>
    /// Gets or initializes the range values for between filter.
    /// </summary>
    [RequiredIf(nameof(Operator), FilterOperator.Between)]
    public string? ValueTo { get; init; }
}

/// <summary>
/// Specifies the operators for filtering conditions.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Specifies the equality operator.
    /// </summary>
    [Description("Equals")]
    Eq,

    /// <summary>
    /// Specifies the "not equals" operator.
    /// </summary>
    [Description("NotEquals")]
    Ne,

    /// <summary>
    /// Specifies the "contains" operator.
    /// </summary>
    Contains,

    /// <summary>
    /// Specifies the "starts with" operator.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Specifies the "ends with" operator.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Specifies the "greater than" operator.
    /// </summary>
    [Description("GreaterThan")]
    Gt,

    /// <summary>
    /// Specifies the "greater than or equals" operator.
    /// </summary>
    [Description("GreaterThanOrEquals")]
    Gte,

    /// <summary>
    /// Specifies the "less than" operator.
    /// </summary>
    [Description("LessThan")]
    Lt,

    /// <summary>
    /// Specifies the "less than or equals" operator.
    /// </summary>
    [Description("LessThanOrEqual")]
    Lte,

    /// <summary>
    /// Specifies the "between" operator.
    /// </summary>
    Between
}