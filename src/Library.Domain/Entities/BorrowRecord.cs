using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class BorrowRecord : Entity
{
    public Guid BookCopyId { get; private init; }

    public Guid MemberId { get; private init; }

    public DateTime BorrowedAt { get; private init; }

    public DateTime DueAt { get; private init; }

    public DateTime? ReturnedAt { get; private set; }

    public BorrowStatus Status { get; private set; }

    // EF Core materialisation only.
    private BorrowRecord()
    {
    }

    public BorrowRecord(
        Guid id,
        Guid bookCopyId,
        Guid memberId,
        DateTime borrowedAt,
        DateTime dueAt)
        : base(id)
    {
        BookCopyId = bookCopyId;
        MemberId = memberId;
        BorrowedAt = borrowedAt;
        DueAt = dueAt;
        Status = BorrowStatus.Active;
    }

    public void Return(DateTime returnedAt)
    {
        if (Status != BorrowStatus.Active)
            throw new InvalidOperationException("Borrow record is not active.");

        ReturnedAt = returnedAt;
        Status = BorrowStatus.Returned;
    }

    /// <summary>
    /// True when this borrow is still active and its due date has already
    /// passed as of <paramref name="asOfUtc"/>.
    /// </summary>
    public bool IsOverdue(DateTime asOfUtc)
    {
        return Status == BorrowStatus.Active && DueAt < asOfUtc;
    }
}
