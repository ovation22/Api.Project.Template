using Api.Project.Template.Application.Common.Pagination;

namespace Api.Project.Template.Application.Common.Specifications;

public class FilterSpecification<T> : PaginatedSpecification<T> where T : class
{
    /// <summary>
    /// Public constructor for callers that don't provide property mappings.
    /// Callers (code that constructs specs) should use this overload.
    /// </summary>
    public FilterSpecification(
        PaginationRequest request,
        string? defaultSortBy = null,
        SortDirection? defaultSortDirection = null)
        : this(request, propertyMappings: null, defaultSortBy, defaultSortDirection)
    {
    }

    /// <summary>
    /// Protected constructor for derived classes to supply per-type mappings (and optional default sort).
    /// Derived specs should call this constructor and pass their static mapping dictionary.
    /// This avoids calling virtual members from the base constructor.
    /// </summary>
    protected FilterSpecification(
        PaginationRequest request,
        Dictionary<string, string>? propertyMappings,
        string? defaultSortBy = null,
        SortDirection? defaultSortDirection = null)
        : base(request.Page, request.Size)
    {
        var mappings = propertyMappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        ConfigureFromRequest(
            request,
            mappings,
            () => ApplyAndFilters(request.Filters!, mappings),
            () => ApplyOrFilters(request.Filters!, mappings),
            (sortBy, direction, map) => ApplySorting(sortBy, direction, map),
            defaultSortBy,
            defaultSortDirection ?? SortDirection.Asc);
    }

    // Shared helper used by both non-projected and projected variants.
    private static void ConfigureFromRequest(
        PaginationRequest request,
        Dictionary<string, string> mappings,
        Action applyAnd,
        Action applyOr,
        Action<string, SortDirection, Dictionary<string, string>> applySort,
        string? defaultSortBy,
        SortDirection defaultSortDirection)
    {
        if (request.Filters != null && request.Filters.Any())
        {
            if (request.Operator == LogicalOperator.And)
                applyAnd();
            else
                applyOr();
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            applySort(request.SortBy!, request.Direction, mappings);
        }
        else
        {
            var sortBy = defaultSortBy;
            var sortDirection = defaultSortDirection;

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                applySort(sortBy, sortDirection, mappings);
            }
        }
    }
}

public class FilterSpecification<T, TResult> : PaginatedSpecification<T, TResult> where T : class
{
    /// <summary>
    /// Public ctor for callers that don't provide property mappings.
    /// Use this when you don't need to supply per-type mappings (most call sites).
    /// </summary>
    public FilterSpecification(
        PaginationRequest request,
        string? defaultSortBy = null,
        SortDirection? defaultSortDirection = null)
        : this(request, propertyMappings: null, defaultSortBy, defaultSortDirection)
    {
    }

    /// <summary>
    /// Protected ctor for derived classes to supply per-type mappings (and optional default sort).
    /// Derived specs should call this ctor and pass their static mapping dictionary to avoid
    /// calling virtual members from the base constructor.
    /// </summary>
    protected FilterSpecification(
        PaginationRequest request,
        Dictionary<string, string>? propertyMappings,
        string? defaultSortBy = null,
        SortDirection? defaultSortDirection = null)
        : base(request.Page, request.Size)
    {
        var mappings = propertyMappings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (request.Filters != null && request.Filters.Any())
        {
            if (request.Operator == LogicalOperator.And)
                ApplyAndFilters(request.Filters, mappings);
            else
                ApplyOrFilters(request.Filters, mappings);
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            ApplySorting(request.SortBy, request.Direction, mappings);
        }
        else
        {
            var sortBy = defaultSortBy;
            var sortDirection = defaultSortDirection ?? SortDirection.Asc;

            if (!string.IsNullOrWhiteSpace(sortBy))
                ApplySorting(sortBy, sortDirection, mappings);
        }
    }
}