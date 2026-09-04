using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Common.Search;
using Library.Application.Features.Borrowing.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.Borrowing;

public sealed class BorrowingService(
    IMemberRepository memberRepository,
    IBookCopyRepository bookCopyRepository,
    IBorrowRecordRepository borrowRecordRepository,
    IUnitOfWork unitOfWork)
{
    public Result<PagedResult<BorrowRecordResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(
            borrowRecordRepository.Query(), request, BorrowSearchMap.Fields);

        return result.IsSuccess
            ? Result.Success(result.Value!.Map(Map))
            : Result.Failure<PagedResult<BorrowRecordResponse>>(result.Errors);
    }

    public async Task<BorrowRecordResponse> IssueAsync(
        IssueBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(
            request.MemberId,
            cancellationToken);

        if (member is null)
            throw new KeyNotFoundException("Member was not found.");

        if (!member.CanBorrow())
            throw new InvalidOperationException(
                "Member is not allowed to borrow books.");

        if (await borrowRecordRepository.HasActiveBorrowAsync(
                member.Id,
                cancellationToken))
            throw new InvalidOperationException(
                "Member already has an active borrowed book. " +
                "Only one active borrow is allowed per member.");

        var copy = await bookCopyRepository.GetByIdAsync(
            request.BookCopyId,
            cancellationToken);

        if (copy is null)
            throw new KeyNotFoundException("Book copy was not found.");

        var borrowedAt = DateTime.UtcNow;

        if (request.DueAt <= borrowedAt)
            throw new ArgumentException(
                "Due date must be in the future.");

        copy.Issue();

        var record = new BorrowRecord(
            Guid.NewGuid(),
            copy.Id,
            member.Id,
            borrowedAt,
            request.DueAt);

        // Copy-status change and record insert must be atomic.
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await bookCopyRepository.UpdateAsync(
            copy,
            cancellationToken);

        await borrowRecordRepository.AddAsync(
            record,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Map(record);
    }

    public async Task<BorrowRecordResponse> ReturnAsync(
        Guid borrowRecordId,
        ReturnBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var record = await borrowRecordRepository.GetByIdAsync(
            borrowRecordId,
            cancellationToken);

        if (record is null)
            throw new KeyNotFoundException(
                "Borrow record was not found.");

        var copy = await bookCopyRepository.GetByIdAsync(
            record.BookCopyId,
            cancellationToken);

        if (copy is null)
            throw new KeyNotFoundException(
                "Book copy was not found.");

        var returnedAt = request.ReturnedAt ?? DateTime.UtcNow;

        copy.Return();
        record.Return(returnedAt);

        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await bookCopyRepository.UpdateAsync(
            copy,
            cancellationToken);

        await borrowRecordRepository.UpdateAsync(
            record,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Map(record);
    }

    private static BorrowRecordResponse Map(
        BorrowRecord record)
    {
        return new BorrowRecordResponse(
            record.Id,
            record.MemberId,
            record.BookCopyId,
            record.BorrowedAt,
            record.DueAt,
            record.ReturnedAt,
            record.Status);
    }
}
