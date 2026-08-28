using Library.Application.Common.Pagination;

namespace Library.Application.Features.Books.Models;

/// <summary>
/// Defines filtering, sorting and pagination options for the book catalog.
/// </summary>
public sealed record BookQuery(
    int PageNumber = PaginationDefaults.DefaultPageNumber,
    int PageSize = PaginationDefaults.DefaultPageSize,
    string? Search = null,
    string? SearchBy = null,
    string? SortBy = null,
    string? SortDirection = null);