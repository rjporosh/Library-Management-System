using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Pagination;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.Books;

public sealed class BookService(IBookRepository bookRepository)
{
    public async Task<PagedBookResponse> GetAllAsync(
        BookQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(
            query.PageNumber,
            PaginationDefaults.DefaultPageNumber);
        
        var normalizedPageSize = Math.Clamp(
            query.PageSize,
            PaginationDefaults.MinPageSize,
            PaginationDefaults.MaxPageSize);

        var normalizedQuery = query with
        {
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };

        var (books, totalItems) = await bookRepository.GetAsync(
            normalizedQuery,
            cancellationToken);

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(
                totalItems / (double)normalizedPageSize);

        var items = books
            .Select(Map)
            .ToList();

        return new PagedBookResponse(
            items,
            normalizedPageNumber,
            normalizedPageSize,
            totalItems,
            totalPages,
            normalizedPageNumber < totalPages,
            normalizedPageNumber > 1);
    }
    public async Task<BookResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(
            id,
            cancellationToken);

        return book is null ? null : Map(book);
    }

    public async Task<BookResponse> CreateAsync(
        CreateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var book = new Book(
            Guid.NewGuid(),
            request.ISBN,
            request.Title,
            request.Author,
            request.PublishedYear,
            request.Description);

        await bookRepository.AddAsync(
            book,
            cancellationToken);

        return Map(book);
    }

    private static BookResponse Map(Book book)
    {
        return new BookResponse(
            book.Id,
            book.ISBN,
            book.Title,
            book.Author,
            book.Description,
            book.PublishedYear);
    }
}