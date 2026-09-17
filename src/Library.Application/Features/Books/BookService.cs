using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Common.Search;
using Library.Application.Common.Validation;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.Books;

public sealed class BookService(
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository,
    IBorrowRecordRepository borrowRecordRepository,
    IUnitOfWork unitOfWork)
{
    public Result<PagedResult<BookResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(bookRepository.Query(), request, BookSearchMap.Fields);

        return result.IsSuccess
            ? Result.Success(result.Value!.Map(Map))
            : Result.Failure<PagedResult<BookResponse>>(result.Errors);
    }

    public async Task<PagedBookResponse> GetAllAsync(BookQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, PaginationDefaults.DefaultPageNumber);
        var pageSize = Math.Clamp(query.PageSize, PaginationDefaults.MinPageSize, PaginationDefaults.MaxPageSize);

        var (books, totalItems) = await bookRepository.GetAsync(
            query with { PageNumber = pageNumber, PageSize = pageSize }, cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedBookResponse(
            [.. books.Select(Map)], pageNumber, pageSize, totalItems, totalPages,
            pageNumber < totalPages, pageNumber > 1);
    }

    public async Task<BookResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(id, cancellationToken);
        return book is null ? null : Map(book);
    }

    public async Task<BookResponse> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(
            new BookCandidate(request.ISBN, request.Title, request.Author, request.PublishedYear,
                request.Category, request.Publisher, request.Description,
                request.HasEbook, request.EbookUrl, request.HasAudiobook, request.AudiobookUrl),
            null, cancellationToken);

        if (request.TotalCopies < 0)
        {
            throw new ValidationException([
                new ApiError(ErrorCodes.BookTotalCopiesInvalid, "Total copies cannot be negative.", "totalCopies")
            ]);
        }

        var book = new Book(
            Guid.NewGuid(), request.ISBN.Trim(), request.Title.Trim(), request.Author.Trim(),
            request.PublishedYear, request.Description?.Trim(),
            request.Category.Trim(), request.Publisher.Trim(),
            request.CoverImageUrl?.Trim(), request.Edition?.Trim(),
            request.HasEbook, request.EbookUrl?.Trim(),
            request.HasAudiobook, request.AudiobookUrl?.Trim(),
            request.ExternalBuyUrl?.Trim(), request.ExternalPdfUrl?.Trim());

        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await bookRepository.AddAsync(book, cancellationToken);

        if (request.TotalCopies > 0)
        {
            await bookCopyRepository.AddRangeAsync(
                await GenerateSequentialCopiesAsync(book.Id, request.TotalCopies, cancellationToken),
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Map(book);
    }

    public async Task<BookResponse?> UpdateAsync(Guid id, UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return null;
        }

        await ValidateAsync(
            new BookCandidate(request.ISBN, request.Title, request.Author, request.PublishedYear,
                request.Category, request.Publisher, request.Description,
                request.HasEbook, request.EbookUrl, request.HasAudiobook, request.AudiobookUrl),
            id, cancellationToken);

        book.Update(
            request.ISBN.Trim(), request.Title.Trim(), request.Author.Trim(), request.PublishedYear,
            request.Description?.Trim(), request.Category.Trim(), request.Publisher.Trim(),
            request.CoverImageUrl?.Trim(), request.Edition?.Trim(),
            request.HasEbook, request.EbookUrl?.Trim(),
            request.HasAudiobook, request.AudiobookUrl?.Trim(),
            request.ExternalBuyUrl?.Trim(), request.ExternalPdfUrl?.Trim());

        await bookRepository.UpdateAsync(book, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(book);
    }

    /// <summary>Retrieves a book with its "smart availability" resolution (physical -> ebook -> audiobook -> external suggestion).</summary>
    public async Task<BookDetailResponse?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return null;
        }

        var copies = await bookCopyRepository.GetByBookIdAsync(id, cancellationToken);
        var totalCopies = copies.Count;
        var availableCopies = copies.Count(c => c.Status == BookCopyStatus.Available);

        var availability = availableCopies > 0
            ? new BookAvailabilitySummary(BookAvailabilityStatus.PhysicalAvailable, totalCopies, availableCopies, null, null, null)
            : book.HasEbook
                ? new BookAvailabilitySummary(BookAvailabilityStatus.Ebook, totalCopies, availableCopies, book.EbookUrl, null, null)
                : book.HasAudiobook
                    ? new BookAvailabilitySummary(BookAvailabilityStatus.Audiobook, totalCopies, availableCopies, book.AudiobookUrl, null, null)
                    : new BookAvailabilitySummary(BookAvailabilityStatus.Unavailable, totalCopies, availableCopies, null, book.ExternalBuyUrl, book.ExternalPdfUrl);

        return new BookDetailResponse(Map(book), availability);
    }

    private async Task<List<BookCopy>> GenerateSequentialCopiesAsync(Guid bookId, int count, CancellationToken cancellationToken)
    {
        const string prefix = "BC-";
        var start = await bookCopyRepository.GetMaxBarcodeNumberAsync(prefix, cancellationToken);

        var copies = new List<BookCopy>(count);
        for (var i = 1; i <= count; i++)
        {
            copies.Add(new BookCopy(Guid.NewGuid(), bookId, $"{prefix}{start + i:D4}"));
        }

        return copies;
    }

    /// <summary>
    /// Deletes a book (soft delete). Smart cascade:
    ///  - any copy currently borrowed -> blocked, always;
    ///  - copies exist and <paramref name="force"/> is false -> asks the caller
    ///    to confirm the cascade (returns BOOK_HAS_DEPENDENT_COPIES);
    ///  - else soft-deletes the book and all its copies in one transaction.
    /// </summary>
    public async Task<Result> DeleteAsync(Guid id, bool force = false, CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            return Result.Failure(new ApiError(ErrorCodes.BookNotFound, "Book was not found.", "id"));
        }

        var copies = await bookCopyRepository.GetByBookIdAsync(id, cancellationToken);

        var borrowed = new List<BookCopy>();
        foreach (var copy in copies)
        {
            if (copy.Status == BookCopyStatus.Borrowed
                || await borrowRecordRepository.HasActiveBorrowForCopyAsync(copy.Id, cancellationToken))
            {
                borrowed.Add(copy);
            }
        }

        if (borrowed.Count > 0)
        {
            return Result.Failure(new ApiError(
                ErrorCodes.BookHasBorrowedCopies,
                $"“{book.Title}” cannot be deleted: {borrowed.Count} copy(ies) are currently borrowed " +
                $"({string.Join(", ", borrowed.Select(c => c.Barcode))}). Return them first.",
                "id"));
        }

        if (copies.Count > 0 && !force)
        {
            return Result.Failure(new ApiError(
                ErrorCodes.BookHasDependentCopies,
                $"“{book.Title}” has {copies.Count} book copy(ies) attached. Deleting the book will also " +
                "delete all of them. Confirm to proceed.",
                "id", null, null, string.Join(", ", copies.Select(c => c.Barcode))));
        }

        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        foreach (var copy in copies)
        {
            await bookCopyRepository.DeleteAsync(copy, cancellationToken);
        }

        await bookRepository.DeleteAsync(book, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Result.Success();
    }

    private async Task ValidateAsync(BookCandidate candidate, Guid? excludingId, CancellationToken cancellationToken)
    {
        var errors = new List<ApiError>(BookValidator.Validate(candidate));

        if (!string.IsNullOrWhiteSpace(candidate.Isbn)
            && await bookRepository.ExistsByIsbnAsync(candidate.Isbn.Trim(), excludingId, cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.BookIsbnDuplicate,
                $"A book with ISBN '{candidate.Isbn}' already exists.", "isbn"));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static BookResponse Map(Book book) =>
        new(book.Id, book.ISBN, book.Title, book.Author, book.Category, book.Publisher,
            book.Description, book.PublishedYear, book.CoverImageUrl, book.Edition,
            book.HasEbook, book.EbookUrl, book.HasAudiobook, book.AudiobookUrl,
            book.ExternalBuyUrl, book.ExternalPdfUrl);
}
