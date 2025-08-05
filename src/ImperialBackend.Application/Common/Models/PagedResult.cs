namespace ImperialBackend.Application.Common.Models;

/// <summary>
/// Represents a paged result containing a subset of items and pagination metadata
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the PagedResult class
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="totalCount">The total number of items across all pages</param>
    /// <param name="pageNumber">The current page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items?.ToList() ?? new List<T>();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        HasPrevious = pageNumber > 1;
        HasNext = pageNumber < TotalPages;
    }

    /// <summary>
    /// Gets the items in the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the total number of items across all pages
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the current page number (1-based)
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of pages
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page
    /// </summary>
    public bool HasPrevious { get; }

    /// <summary>
    /// Gets a value indicating whether there is a next page
    /// </summary>
    public bool HasNext { get; }

    /// <summary>
    /// Gets the number of items in the current page
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>An empty paged result</returns>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResult<T>(Enumerable.Empty<T>(), 0, pageNumber, pageSize);
    }
}