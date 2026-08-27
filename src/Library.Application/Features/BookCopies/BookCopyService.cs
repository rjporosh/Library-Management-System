using Library.Application.Abstractions.Persistence;
using Library.Application.Features.BookCopies.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.BookCopies;

public sealed class BookCopyService(
    IBookCopyRepository bookCopyRepository)
{
    public async Task<IReadOnlyList<BookCopyResponse>> GetByBookIdAsync(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var copies = await bookCopyRepository.GetByBookIdAsync(
            bookId,
            cancellationToken);

        return copies
            .Select(Map)
            .ToList();
    }

    public async Task<BookCopyResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var copy = await bookCopyRepository.GetByIdAsync(
            id,
            cancellationToken);

        return copy is null ? null : Map(copy);
    }

    public async Task<BookCopyResponse> CreateAsync(
        CreateBookCopyRequest request,
        CancellationToken cancellationToken = default)
    {
        var copy = new BookCopy(
            Guid.NewGuid(),
            request.BookId,
            request.Barcode);

        await bookCopyRepository.AddAsync(
            copy,
            cancellationToken);

        return Map(copy);
    }

    private static BookCopyResponse Map(BookCopy copy)
    {
        return new BookCopyResponse(
            copy.Id,
            copy.BookId,
            copy.Barcode,
            copy.Status);
    }
}