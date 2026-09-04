using Library.Application.Abstractions.Persistence;
using Library.Domain.Enums;

namespace Library.Application.Features.Dashboard;

/// <summary>Live aggregate figures for the dashboard - computed in one pass, no N+1.</summary>
public sealed class DashboardService(
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository,
    IMemberRepository memberRepository,
    IBorrowRecordRepository borrowRecordRepository)
{
    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var books = bookRepository.Query().Count();
        var copies = bookCopyRepository.Query().ToList();
        var members = await memberRepository.GetAllAsync(cancellationToken);
        var borrows = await borrowRecordRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var recent = borrows
            .OrderByDescending(b => b.BorrowedAt)
            .Take(5)
            .Select(b => new RecentBorrowActivity(b.Id, b.MemberId, b.BookCopyId, b.BorrowedAt, b.DueAt, b.Status))
            .ToList();

        return new DashboardSnapshot(
            TotalBooks: books,
            TotalCopies: copies.Count,
            AvailableCopies: copies.Count(c => c.Status == BookCopyStatus.Available),
            BorrowedCopies: copies.Count(c => c.Status == BookCopyStatus.Borrowed),
            OutOfServiceCopies: copies.Count(c => c.Status is BookCopyStatus.Lost or BookCopyStatus.Damaged or BookCopyStatus.Maintenance),
            TotalMembers: members.Count,
            ActiveMembers: members.Count(m => m.Status == MemberStatus.Active),
            SuspendedMembers: members.Count(m => m.Status == MemberStatus.Suspended),
            InactiveMembers: members.Count(m => m.Status == MemberStatus.Inactive),
            MembersExpiringSoon: members.Count(m => m.Status == MemberStatus.Active && m.MembershipExpiresAt <= now.AddDays(30)),
            ActiveBorrows: borrows.Count(b => b.Status == BorrowStatus.Active),
            OverdueBorrows: borrows.Count(b => b.IsOverdue(now)),
            RecentActivity: recent);
    }
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
    BorrowStatus Status);
