using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class BorrowRecord
{
    public Guid Id { get; init; }

    public Guid BookCopyId { get; init; }

    public Guid MemberId { get; init; }

    public DateTime BorrowedAt { get; init; }

    public DateTime DueAt { get; init; }

    public DateTime? ReturnedAt { get; private set; }

    public BorrowStatus Status { get; private set; }

    public BorrowRecord(
        Guid id,
        Guid bookCopyId,
        Guid memberId,
        DateTime borrowedAt,
        DateTime dueAt)
    {
        if (dueAt <= borrowedAt)
            throw new ArgumentException(
                "Due date must be after borrowed date.");

        Id = id;
        BookCopyId = bookCopyId;
        MemberId = memberId;
        BorrowedAt = borrowedAt;
        DueAt = dueAt;
        Status = BorrowStatus.Active;
    }

    public void Return(DateTime returnedAt)
    {
        if (Status != BorrowStatus.Active)
            throw new InvalidOperationException(
                "Borrow record is not active.");

        ReturnedAt = returnedAt;
        Status = BorrowStatus.Returned;
    }
}