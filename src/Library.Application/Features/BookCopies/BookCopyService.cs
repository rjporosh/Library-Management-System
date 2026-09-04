using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Common.Search;
using Library.Application.Features.BookCopies.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.BookCopies;

public sealed class BookCopyService(
    IBookCopyRepository bookCopyRepository,
    IBookRepository bookRepository,
    IBorrowRecordRepository borrowRecordRepository,
    IUnitOfWork unitOfWork)
{
    public Result<PagedResult<BookCopyResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(bookCopyRepository.Query(), request, BookCopySearchMap.Fields);

        return result.IsSuccess
            ? Result.Success(result.Value!.Map(Map))
            : Result.Failure<PagedResult<BookCopyResponse>>(result.Errors);
    }

    public async Task<IReadOnlyList<BookCopyResponse>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var copies = await bookCopyRepository.GetByBookIdAsync(bookId, cancellationToken);
        return copies.Select(Map).ToList();
    }

    public async Task<BookCopyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var copy = await bookCopyRepository.GetByIdAsync(id, cancellationToken);
        return copy is null ? null : Map(copy);
    }

    public async Task<Result<BookCopyResponse>> CreateAsync(CreateBookCopyRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ApiError>();

        if (request.BookId == Guid.Empty)
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBookRequired, "A book must be specified.", "bookId", Required: true));
        }
        else if (await bookRepository.GetByIdAsync(request.BookId, cancellationToken) is null)
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBookNotFound, "The specified book does not exist.", "bookId"));
        }

        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBarcodeRequired, "Barcode is required.", "barcode", Required: true));
        }
        else if (await bookCopyRepository.ExistsByBarcodeAsync(request.Barcode.Trim(), null, cancellationToken))
        {
            errors.Add(new ApiError(ErrorCodes.BookCopyBarcodeDuplicate, $"Barcode '{request.Barcode}' is already in use.", "barcode"));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<BookCopyResponse>(errors);
        }

        var copy = new BookCopy(Guid.NewGuid(), request.BookId, request.Barcode.Trim());
        await bookCopyRepository.AddAsync(copy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(copy));
    }

    public async Task<Result<BookCopyResponse>> UpdateAsync(Guid id, UpdateBookCopyRequest request, CancellationToken cancellationToken = default)
    {
        var copy = await bookCopyRepository.GetByIdAsync(id, cancellationToken);
        if (copy is null)
        {
            return Result.Failure<BookCopyResponse>(new ApiError(ErrorCodes.BookCopyNotFound, "Book copy was not found.", "id"));
        }

        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return Result.Failure<BookCopyResponse>(new ApiError(ErrorCodes.BookCopyBarcodeRequired, "Barcode is required.", "barcode", Required: true));
        }

        if (await bookCopyRepository.ExistsByBarcodeAsync(request.Barcode.Trim(), id, cancellationToken))
        {
            return Result.Failure<BookCopyResponse>(new ApiError(ErrorCodes.BookCopyBarcodeDuplicate, $"Barcode '{request.Barcode}' is already in use.", "barcode"));
        }

        copy.ChangeBarcode(request.Barcode.Trim());
        await bookCopyRepository.UpdateAsync(copy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(copy));
    }

    public async Task<Result<BookCopyResponse>> ChangeStatusAsync(Guid id, ChangeBookCopyStatusRequest request, CancellationToken cancellationToken = default)
    {
        var copy = await bookCopyRepository.GetByIdAsync(id, cancellationToken);
        if (copy is null)
        {
            return Result.Failure<BookCopyResponse>(new ApiError(ErrorCodes.BookCopyNotFound, "Book copy was not found.", "id"));
        }

        if (!Enum.TryParse<BookCopyStatus>(request.Status, ignoreCase: true, out var status))
        {
            return Result.Failure<BookCopyResponse>(new ApiError(
                ErrorCodes.BookCopyStatusInvalid, $"'{request.Status}' is not a valid status.", "status",
                SupportedValues: string.Join(", ", Enum.GetNames<BookCopyStatus>())));
        }

        try
        {
            copy.ChangeStatus(status);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<BookCopyResponse>(new ApiError(ErrorCodes.BookCopyBorrowed, ex.Message, "status"));
        }

        await bookCopyRepository.UpdateAsync(copy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(copy));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var copy = await bookCopyRepository.GetByIdAsync(id, cancellationToken);
        if (copy is null)
        {
            return Result.Failure(new ApiError(ErrorCodes.BookCopyNotFound, "Book copy was not found.", "id"));
        }

        if (copy.Status == BookCopyStatus.Borrowed ||
            await borrowRecordRepository.HasActiveBorrowForCopyAsync(id, cancellationToken))
        {
            return Result.Failure(new ApiError(ErrorCodes.BookCopyBorrowed,
                "This copy is currently borrowed and cannot be deleted.", "id"));
        }

        await bookCopyRepository.DeleteAsync(copy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static BookCopyResponse Map(BookCopy copy) =>
        new(copy.Id, copy.BookId, copy.Barcode, copy.Status);
}
