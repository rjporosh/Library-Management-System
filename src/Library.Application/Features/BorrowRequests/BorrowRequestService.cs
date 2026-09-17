using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Errors;
using Library.Application.Common.Pagination;
using Library.Application.Common.Results;
using Library.Application.Common.Search;
using Library.Application.Features.Borrowing;
using Library.Application.Features.Borrowing.Models;
using Library.Application.Features.BorrowRequests.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Features.BorrowRequests;

public sealed class BorrowRequestService(
    IBorrowRequestRepository borrowRequestRepository,
    IMemberRepository memberRepository,
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository,
    IUnitOfWork unitOfWork,
    BorrowingService borrowingService)
{
    public Result<PagedResult<BorrowRequestResponse>> Search(SearchRequest request)
    {
        var result = QueryableSearchBuilder.Apply(borrowRequestRepository.Query(), request, BorrowRequestSearchMap.Fields);
        if (!result.IsSuccess)
        {
            return Result.Failure<PagedResult<BorrowRequestResponse>>(result.Errors);
        }

        var page = result.Value!;

        var memberIds = page.Items.Select(r => r.MemberId).ToHashSet();
        var members = memberRepository.Query().Where(m => memberIds.Contains(m.Id)).ToDictionary(m => m.Id);

        var bookIds = page.Items.Where(r => r.BookId is not null).Select(r => r.BookId!.Value).ToHashSet();
        var books = bookRepository.Query().Where(b => bookIds.Contains(b.Id)).ToDictionary(b => b.Id);

        return Result.Success(page.Map(r =>
        {
            members.TryGetValue(r.MemberId, out var member);
            var bookTitle = r.BookId is { } bookId && books.TryGetValue(bookId, out var book) ? book.Title : null;
            return Map(r, member?.Name, member?.MembershipNumber, bookTitle);
        }));
    }

    public async Task<IReadOnlyList<BorrowRequestResponse>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var requests = await borrowRequestRepository.GetByMemberIdAsync(memberId, cancellationToken);
        var member = await memberRepository.GetByIdAsync(memberId, cancellationToken);

        var bookIds = requests.Where(r => r.BookId is not null).Select(r => r.BookId!.Value).ToHashSet();
        var books = bookRepository.Query().Where(b => bookIds.Contains(b.Id)).ToDictionary(b => b.Id);

        return [.. requests.OrderByDescending(r => r.RequestedAt).Select(r =>
        {
            var bookTitle = r.BookId is { } bookId && books.TryGetValue(bookId, out var book) ? book.Title : null;
            return Map(r, member?.Name, member?.MembershipNumber, bookTitle);
        })];
    }

    public async Task<Result<BorrowRequestResponse>> CreateAsync(
        Guid memberId, CreateBorrowRequestRequest request, CancellationToken cancellationToken = default)
    {
        var member = await memberRepository.GetByIdAsync(memberId, cancellationToken);
        if (member is null)
        {
            return Result.Failure<BorrowRequestResponse>(new ApiError(ErrorCodes.MemberNotFound, "Member was not found.", "memberId"));
        }

        BorrowRequest borrowRequest;

        if (request.Type == BorrowRequestType.Borrow)
        {
            if (request.BookId is null)
            {
                return Result.Failure<BorrowRequestResponse>(
                    new ApiError(ErrorCodes.BorrowRequestBookRequired, "A book is required to request a borrow.", "bookId", Required: true));
            }

            var book = await bookRepository.GetByIdAsync(request.BookId.Value, cancellationToken);
            if (book is null)
            {
                return Result.Failure<BorrowRequestResponse>(
                    new ApiError(ErrorCodes.BorrowRequestBookNotFound, "Book was not found.", "bookId"));
            }

            if (await borrowRequestRepository.HasPendingBorrowRequestAsync(memberId, request.BookId.Value, cancellationToken))
            {
                return Result.Failure<BorrowRequestResponse>(
                    new ApiError(ErrorCodes.BorrowRequestDuplicate, "A pending request for this book already exists.", "bookId"));
            }

            borrowRequest = BorrowRequest.ForBorrow(Guid.NewGuid(), memberId, request.BookId.Value, request.Note?.Trim());
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.SuggestedTitle))
            {
                return Result.Failure<BorrowRequestResponse>(
                    new ApiError(ErrorCodes.BorrowRequestTitleRequired, "A title is required to suggest a purchase.", "suggestedTitle", Required: true));
            }

            borrowRequest = BorrowRequest.ForPurchase(
                Guid.NewGuid(), memberId, request.SuggestedTitle.Trim(), request.SuggestedAuthor?.Trim(), request.Note?.Trim());
        }

        await borrowRequestRepository.AddAsync(borrowRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await MapWithMemberAsync(borrowRequest, member, cancellationToken));
    }

    /// <summary>
    /// Approves a request. A Borrow request picks the first available copy
    /// of the requested book and actually issues it (via <see cref="Borrowing.BorrowingService"/>
    /// - same rules: active/limit/availability all apply); a Purchase
    /// request is simply marked Approved for the librarian to action.
    /// </summary>
    public async Task<Result<BorrowRequestResponse>> ApproveAsync(Guid id, Guid decidedByUserId, CancellationToken cancellationToken = default)
    {
        var borrowRequest = await borrowRequestRepository.GetByIdAsync(id, cancellationToken);
        if (borrowRequest is null)
        {
            return Result.Failure<BorrowRequestResponse>(new ApiError(ErrorCodes.BorrowRequestNotFound, "Request was not found.", "id"));
        }

        if (borrowRequest.Status != BorrowRequestStatus.Pending)
        {
            return Result.Failure<BorrowRequestResponse>(
                new ApiError(ErrorCodes.BorrowRequestAlreadyDecided, "This request has already been decided.", "id"));
        }

        Guid? borrowRecordId = null;

        if (borrowRequest.Type == BorrowRequestType.Borrow)
        {
            var copy = bookCopyRepository.Query()
                .FirstOrDefault(c => c.BookId == borrowRequest.BookId && c.Status == BookCopyStatus.Available);

            if (copy is null)
            {
                return Result.Failure<BorrowRequestResponse>(
                    new ApiError(ErrorCodes.BorrowRequestNoCopyAvailable, "No available copy of this book right now.", "bookId"));
            }

            try
            {
                var issued = await borrowingService.IssueAsync(
                    new IssueBookRequest(borrowRequest.MemberId, copy.Id, DateTime.UtcNow.AddDays(14)),
                    cancellationToken);
                borrowRecordId = issued.Id;
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<BorrowRequestResponse>(new ApiError(ErrorCodes.Conflict, ex.Message, "memberId"));
            }
        }

        borrowRequest.Approve(decidedByUserId, borrowRecordId);
        await borrowRequestRepository.UpdateAsync(borrowRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await MapAsync(borrowRequest, cancellationToken));
    }

    public async Task<Result<BorrowRequestResponse>> RejectAsync(Guid id, Guid decidedByUserId, CancellationToken cancellationToken = default)
    {
        var borrowRequest = await borrowRequestRepository.GetByIdAsync(id, cancellationToken);
        if (borrowRequest is null)
        {
            return Result.Failure<BorrowRequestResponse>(new ApiError(ErrorCodes.BorrowRequestNotFound, "Request was not found.", "id"));
        }

        if (borrowRequest.Status != BorrowRequestStatus.Pending)
        {
            return Result.Failure<BorrowRequestResponse>(
                new ApiError(ErrorCodes.BorrowRequestAlreadyDecided, "This request has already been decided.", "id"));
        }

        borrowRequest.Reject(decidedByUserId);
        await borrowRequestRepository.UpdateAsync(borrowRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await MapAsync(borrowRequest, cancellationToken));
    }

    private async Task<BorrowRequestResponse> MapAsync(BorrowRequest request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(request.MemberId, cancellationToken);
        return await MapWithMemberAsync(request, member, cancellationToken);
    }

    private async Task<BorrowRequestResponse> MapWithMemberAsync(BorrowRequest request, Member? member, CancellationToken cancellationToken)
    {
        string? bookTitle = null;
        if (request.BookId is { } bookId)
        {
            var book = await bookRepository.GetByIdAsync(bookId, cancellationToken);
            bookTitle = book?.Title;
        }

        return Map(request, member?.Name, member?.MembershipNumber, bookTitle);
    }

    private static BorrowRequestResponse Map(BorrowRequest r, string? memberName = null, string? membershipNumber = null, string? bookTitle = null) =>
        new(
            r.Id, r.MemberId, memberName ?? "", membershipNumber ?? "", r.Type, r.BookId, bookTitle,
            r.SuggestedTitle, r.SuggestedAuthor, r.Note, r.Status, r.RequestedAt, r.DecidedAt, r.BorrowRecordId);
}
