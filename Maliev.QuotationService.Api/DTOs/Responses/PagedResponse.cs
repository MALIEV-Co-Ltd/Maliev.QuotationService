namespace Maliev.QuotationService.Api.DTOs.Responses;

/// <summary>
/// Standard pagination response wrapper.
/// </summary>
/// <typeparam name="T">Type of data items.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// The page of data.
    /// </summary>
    public IEnumerable<T> Data { get; set; } = [];

    /// <summary>
    /// Pagination metadata.
    /// </summary>
    public PaginationMeta Meta { get; set; } = new();
}

/// <summary>
/// Metadata for paginated responses.
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total count of items available.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
