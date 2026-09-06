using Library.Application.Features.Dashboard;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories.EfCore;

/// <summary>Dashboard aggregate via EF Core - counts are pushed to SQL; only the 5 recent rows are materialised.</summary>
public sealed class EfDashboardReadStore(LibraryDbContext db) : IDashboardReadStore
{
    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var soon = now.AddDays(30);

        var recent = await db.BorrowRecords.AsNoTracking()
            .OrderByDescending(b => b.BorrowedAt)
            .Take(5)
            .Select(b => new RecentBorrowActivity(b.Id, b.MemberId, b.BookCopyId, b.BorrowedAt, b.DueAt, b.Status))
            .ToListAsync(cancellationToken);

        return new DashboardSnapshot(
            TotalBooks: await db.Books.CountAsync(cancellationToken),
            TotalCopies: await db.BookCopies.CountAsync(cancellationToken),
            AvailableCopies: await db.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Available, cancellationToken),
            BorrowedCopies: await db.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Borrowed, cancellationToken),
            OutOfServiceCopies: await db.BookCopies.CountAsync(
                c => c.Status == BookCopyStatus.Lost || c.Status == BookCopyStatus.Damaged || c.Status == BookCopyStatus.Maintenance,
                cancellationToken),
            TotalMembers: await db.Members.CountAsync(cancellationToken),
            ActiveMembers: await db.Members.CountAsync(m => m.Status == MemberStatus.Active, cancellationToken),
            SuspendedMembers: await db.Members.CountAsync(m => m.Status == MemberStatus.Suspended, cancellationToken),
            InactiveMembers: await db.Members.CountAsync(m => m.Status == MemberStatus.Inactive, cancellationToken),
            MembersExpiringSoon: await db.Members.CountAsync(
                m => m.Status == MemberStatus.Active && m.MembershipExpiresAt <= soon, cancellationToken),
            ActiveBorrows: await db.BorrowRecords.CountAsync(b => b.Status == BorrowStatus.Active, cancellationToken),
            OverdueBorrows: await db.BorrowRecords.CountAsync(b => b.Status == BorrowStatus.Active && b.DueAt < now, cancellationToken),
            RecentActivity: recent);
    }
}
