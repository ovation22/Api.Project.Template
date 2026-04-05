namespace Api.Project.Template.Application.Common.Pagination;

/// <summary>
/// Represents a paginated list of items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class PagedList<T>
{
    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the size of the page.
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    /// Gets the total count of items.
    /// </summary>
    public int Total { get; private set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IEnumerable<T> Data { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedList{T}"/> class.
    /// </summary>
    /// <param name="data">The data to include in the list.</param>
    /// <param name="total">The total count of data.</param>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="size">The size of the page.</param>
    public PagedList(IEnumerable<T> data, int total, int pageNumber, int size)
    {
        PageNumber = pageNumber;
        Size = size;
        Total = total;
        TotalPages = (int)Math.Ceiling(total / (double)size);
        Data = data;
    }
}