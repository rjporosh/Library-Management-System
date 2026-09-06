using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Dashboard;
using Library.Domain.Enums;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

/// <summary>Dashboard aggregate over the in-memory repositories.</summary>
public sealed class InMemoryDashboardReadStore(
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository,
    IMemberRepository memberRepository,
    IBorrowRecordRepository borrowRecordRepository) : IDashboardReadStore
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
            books,
            copies.Count,
            copies.Count(c => c.Status == BookCopyStatus.Available),
            copies.Count(c => c.Status == BookCopyStatus.Borrowed),
            copies.Count(c => c.Status is BookCopyStatus.Lost or BookCopyStatus.Damaged or BookCopyStatus.Maintenance),
            members.Count,
            members.Count(m => m.Status == MemberStatus.Active),
            members.Count(m => m.Status == MemberStatus.Suspended),
            members.Count(m => m.Status == MemberStatus.Inactive),
            members.Count(m => m.Status == MemberStatus.Active && m.MembershipExpiresAt <= now.AddDays(30)),
            borrows.Count(b => b.Status == BorrowStatus.Active),
            borrows.Count(b => b.IsOverdue(now)),
            recent);
    }
}
