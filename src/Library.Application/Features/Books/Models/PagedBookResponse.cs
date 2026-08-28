namespace Library.Application.Features.Books.Models;

/// <summary>
/// Represents a paginated collection of books.
/// </summary>
/// <param name="Items">Books included in the current page.</param>
/// <param name="PageNumber">The current page number.</param>
/// <param name="PageSize">The requested number of items per page.</param>
/// <param name="TotalItems">The total number of matching books.</param>
/// <param name="TotalPages">The total number of available pages.</param>
/// <param name="HasNextPage">Indicates whether another page is available.</param>
/// <param name="HasPreviousPage">Indicates whether a previous page is available.</param>
public sealed record PagedBookResponse(
    IReadOnlyList<BookResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);