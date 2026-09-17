namespace Library.Application.Common.Options;

/// <summary>Bound from the "Borrowing" section of appsettings.</summary>
public sealed class BorrowingOptions
{
    /// <summary>How many books a member may have borrowed (not yet returned) at once.</summary>
    public int MaxActiveBorrowsPerMember { get; set; } = 2;
}
