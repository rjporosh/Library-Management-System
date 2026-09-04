using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Exceptions;
using Library.Application.Features.BookCopies.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.BookCopies;

public sealed class BookCopyService( IBookCopyRepository bookCopyRepository)
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
        var errors = new List<ApiError>();
        if (request.BookId == Guid.Empty)
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBookRequired, "A book must be specified.", "bookId", Required: true));
        }

        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBarcodeRequired, "Barcode is required.", "barcode", Required: true));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

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