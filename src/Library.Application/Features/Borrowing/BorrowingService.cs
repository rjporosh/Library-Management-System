using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Options;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Common.Search;
using Library.Application.Features.Borrowing.Models;
using Library.Domain.Entities;

namespace Library.Application.Features.Borrowing;

public sealed class BorrowingService(
    IMemberRepository memberRepository,
    IBookCopyRepository bookCopyRepository,
    IBookRepository bookRepository,
    IBorrowRecordRepository borrowRecordRepository,
    IUnitOfWork unitOfWork,
    BorrowingOptions? borrowingOptions = null)
{
    private readonly BorrowingOptions _options = borrowingOptions ?? new BorrowingOptions();

    public Result<PagedResult<BorrowRecordResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(
            borrowRecordRepository.Query(), request, BorrowSearchMap.Fields);

        if (!result.IsSuccess)
        {
            return Result.Failure<PagedResult<BorrowRecordResponse>>(result.Errors);
        }

        var page = result.Value!;

        var memberIds = page.Items.Select(r => r.MemberId).ToHashSet();
        var members = memberRepository.Query()
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionary(m => m.Id);

        var copyIds = page.Items.Select(r => r.BookCopyId).ToHashSet();
        var copies = bookCopyRepository.Query()
            .Where(c => copyIds.Contains(c.Id))
            .ToDictionary(c => c.Id);

        var bookIds = copies.Values.Select(c => c.BookId).ToHashSet();
        var books = bookRepository.Query()
            .Where(b => bookIds.Contains(b.Id))
            .ToDictionary(b => b.Id);

        return Result.Success(page.Map(r =>
        {
            members.TryGetValue(r.MemberId, out var member);
            copies.TryGetValue(r.BookCopyId, out var copy);
            Book? book = copy is not null && books.TryGetValue(copy.BookId, out var b) ? b : null;

            return Map(r, member?.Name ?? "", member?.MembershipNumber ?? "", book?.Title ?? "", copy?.Barcode ?? "");
        }));
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

        var activeBorrows = await borrowRecordRepository.CountActiveBorrowsAsync(member.Id, cancellationToken);
        if (activeBorrows >= _options.MaxActiveBorrowsPerMember)
            throw new InvalidOperationException(
                $"Member already has {activeBorrows} active borrow(s). " +
                $"A member may borrow at most {_options.MaxActiveBorrowsPerMember} book(s) at a time.");

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
        BorrowRecord record,
        string memberName = "",
        string membershipNumber = "",
        string bookTitle = "",
        string barcode = "")
    {
        return new BorrowRecordResponse(
            record.Id,
            record.MemberId,
            record.BookCopyId,
            record.BorrowedAt,
            record.DueAt,
            record.ReturnedAt,
            record.Status,
            memberName,
            membershipNumber,
            bookTitle,
            barcode);
    }
}
