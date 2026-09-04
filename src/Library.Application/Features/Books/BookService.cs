using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Exceptions;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Common.Search;
using Library.Application.Common.Validation;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.Books;

public sealed class BookService(IBookRepository bookRepository)
{
    public Result<PagedResult<BookResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(bookRepository.Query(), request, BookSearchMap.Fields);

        return result.IsSuccess
            ? Result.Success(result.Value!.Map(Map))
            : Result.Failure<PagedResult<BookResponse>>(result.Errors);
    }

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
        await ValidateAsync(request.ISBN, request.Title, request.Author, request.PublishedYear, null, cancellationToken);

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

    public async Task<BookResponse?> UpdateAsync(
        Guid id,
        UpdateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (book is null)
        {
            return null;
        }

        await ValidateAsync(request.ISBN, request.Title, request.Author, request.PublishedYear, id, cancellationToken);

        book.Update(
            request.ISBN,
            request.Title,
            request.Author,
            request.PublishedYear,
            request.Description);

        await bookRepository.UpdateAsync(
            book,
            cancellationToken);

        return Map(book);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var book = await bookRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (book is null)
        {
            return false;
        }

        await bookRepository.DeleteAsync(
            book,
            cancellationToken);

        return true;
    }

    private async Task ValidateAsync(
        string? isbn, string? title, string? author, int publishedYear, Guid? excludingId, CancellationToken cancellationToken)
    {
        var errors = new List<ApiError>(
            BookValidator.Validate(new BookCandidate(isbn, title, author, publishedYear, null)));

        if (!string.IsNullOrWhiteSpace(isbn)
            && await bookRepository.ExistsByIsbnAsync(isbn.Trim(), excludingId, cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.BookIsbnDuplicate, $"A book with ISBN '{isbn}' already exists.", "isbn"));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
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
