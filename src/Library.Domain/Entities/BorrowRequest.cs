using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

/// <summary>
/// A member-initiated request: either to borrow a specific catalog title,
/// or to suggest the library acquire one it does not have. A librarian
/// approves or rejects it; approving a Borrow request actually issues the
/// book (see <c>BorrowRequestService.ApproveAsync</c>).
/// </summary>
public sealed class BorrowRequest : Entity
{
    public Guid MemberId { get; private set; }

    public BorrowRequestType Type { get; private set; }

    /// <summary>Set for a Borrow request against an existing catalog title.</summary>
    public Guid? BookId { get; private set; }

    /// <summary>Set for a Purchase request when the title is not yet in the catalog.</summary>
    public string? SuggestedTitle { get; private set; }

    public string? SuggestedAuthor { get; private set; }

    /// <summary>Optional message from the member explaining the request.</summary>
    public string? Note { get; private set; }

    public BorrowRequestStatus Status { get; private set; }

    public DateTime RequestedAt { get; private set; }

    public DateTime? DecidedAt { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    /// <summary>Set once a Borrow request is approved and actually issued.</summary>
    public Guid? BorrowRecordId { get; private set; }

    // EF Core materialisation only.
    private BorrowRequest() { }

    public static BorrowRequest ForBorrow(Guid id, Guid memberId, Guid bookId, string? note) =>
        new()
        {
            Id = id,
            MemberId = memberId,
            Type = BorrowRequestType.Borrow,
            BookId = bookId,
            Note = note,
            Status = BorrowRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
        };

    public static BorrowRequest ForPurchase(Guid id, Guid memberId, string suggestedTitle, string? suggestedAuthor, string? note) =>
        new()
        {
            Id = id,
            MemberId = memberId,
            Type = BorrowRequestType.Purchase,
            SuggestedTitle = suggestedTitle,
            SuggestedAuthor = suggestedAuthor,
            Note = note,
            Status = BorrowRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
        };

    public void Approve(Guid decidedByUserId, Guid? borrowRecordId = null)
    {
        Status = Type == BorrowRequestType.Borrow ? BorrowRequestStatus.Fulfilled : BorrowRequestStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        DecidedByUserId = decidedByUserId;
        BorrowRecordId = borrowRecordId;
    }

    public void Reject(Guid decidedByUserId)
    {
        Status = BorrowRequestStatus.Rejected;
        DecidedAt = DateTime.UtcNow;
        DecidedByUserId = decidedByUserId;
    }
}
