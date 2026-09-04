namespace Library.Application.Common.Pagination;

/// <summary>
/// A single page of results plus the metadata the front-end needs to render
/// pagination controls. Shape matches the existing <c>PagedBookResponse</c>.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalItems)
    {
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<T>(
            items,
            pageNumber,
            pageSize,
            totalItems,
            totalPages,
            pageNumber < totalPages,
            pageNumber > 1);
    }

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(
            [.. Items.Select(selector)],
            PageNumber,
            PageSize,
            TotalItems,
            TotalPages,
            HasNextPage,
            HasPreviousPage);
}
