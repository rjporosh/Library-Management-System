using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.Books;

public sealed class BookService(IBookRepository bookRepository)
{
    public async Task<IReadOnlyList<BookResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var books = await bookRepository.GetAllAsync(cancellationToken);

        return books
            .Select(Map)
            .ToList();
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