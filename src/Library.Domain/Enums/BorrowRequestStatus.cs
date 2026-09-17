namespace Library.Domain.Enums;

public enum BorrowRequestStatus
{
    Pending,
    Approved,
    Rejected,

    /// <summary>A Borrow-type request that was approved and actually issued.</summary>
    Fulfilled
}
