using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Borrowing.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.Borrowing;

public sealed class BorrowingService(
    IMemberRepository memberRepository,
    IBookCopyRepository bookCopyRepository,
    IBorrowRecordRepository borrowRecordRepository)
{
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

        await bookCopyRepository.UpdateAsync(
            copy,
            cancellationToken);

        await borrowRecordRepository.AddAsync(
            record,
            cancellationToken);

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

        await bookCopyRepository.UpdateAsync(
            copy,
            cancellationToken);

        await borrowRecordRepository.UpdateAsync(
            record,
            cancellationToken);

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
