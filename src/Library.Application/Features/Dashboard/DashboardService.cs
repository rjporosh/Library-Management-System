namespace Library.Application.Features.Dashboard;

/// <summary>
/// Live aggregate figures for the dashboard. Delegates to the configured
/// <see cref="IDashboardReadStore"/> (EF Core by default, Dapper when
/// <c>Database:Orm=Dapper</c>).
/// </summary>
public sealed class DashboardService(IDashboardReadStore readStore)
{
    public Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        readStore.GetSnapshotAsync(cancellationToken);
}

public sealed record DashboardSnapshot(
    int TotalBooks,
    int TotalCopies,
    int AvailableCopies,
    int BorrowedCopies,
    int OutOfServiceCopies,
    int TotalMembers,
    int ActiveMembers,
    int SuspendedMembers,
    int InactiveMembers,
    int MembersExpiringSoon,
    int ActiveBorrows,
    int OverdueBorrows,
    IReadOnlyList<RecentBorrowActivity> RecentActivity);

public sealed record RecentBorrowActivity(
    Guid BorrowRecordId,
    Guid MemberId,
    Guid BookCopyId,
    DateTime BorrowedAt,
    DateTime DueAt,
    Library.Domain.Enums.BorrowStatus Status);
