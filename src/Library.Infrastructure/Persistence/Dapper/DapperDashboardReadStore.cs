using Dapper;
using Library.Application.Features.Dashboard;
using Library.Infrastructure.Persistence.Interceptors;

namespace Library.Infrastructure.Persistence.Dapper;

/// <summary>
/// Dashboard aggregate via Dapper: one round-trip of SQL COUNT/CASE aggregates
/// plus one small query for the 5 recent rows. Used when
/// <c>Database:Orm=Dapper</c>. Soft-deleted rows are excluded explicitly.
/// </summary>
public sealed class DapperDashboardReadStore(IDbConnectionFactory connectionFactory) : IDashboardReadStore
{
    private const string AggregateSql = """
        SELECT
          (SELECT COUNT(*) FROM books WHERE "IsDeleted" = FALSE) AS TotalBooks,
          (SELECT COUNT(*) FROM book_copies WHERE "IsDeleted" = FALSE) AS TotalCopies,
          (SELECT COUNT(*) FROM book_copies WHERE "IsDeleted" = FALSE AND "Status" = 'Available') AS AvailableCopies,
          (SELECT COUNT(*) FROM book_copies WHERE "IsDeleted" = FALSE AND "Status" = 'Borrowed') AS BorrowedCopies,
          (SELECT COUNT(*) FROM book_copies WHERE "IsDeleted" = FALSE AND "Status" IN ('Lost','Damaged','Maintenance')) AS OutOfServiceCopies,
          (SELECT COUNT(*) FROM members WHERE "IsDeleted" = FALSE) AS TotalMembers,
          (SELECT COUNT(*) FROM members WHERE "IsDeleted" = FALSE AND "Status" = 'Active') AS ActiveMembers,
          (SELECT COUNT(*) FROM members WHERE "IsDeleted" = FALSE AND "Status" = 'Suspended') AS SuspendedMembers,
          (SELECT COUNT(*) FROM members WHERE "IsDeleted" = FALSE AND "Status" = 'Inactive') AS InactiveMembers,
          (SELECT COUNT(*) FROM members WHERE "IsDeleted" = FALSE AND "Status" = 'Active' AND "MembershipExpiresAt" <= @soon) AS MembersExpiringSoon,
          (SELECT COUNT(*) FROM borrow_records WHERE "IsDeleted" = FALSE AND "Status" = 'Active') AS ActiveBorrows,
          (SELECT COUNT(*) FROM borrow_records WHERE "IsDeleted" = FALSE AND "Status" = 'Active' AND "DueAt" < @now) AS OverdueBorrows
        """;

    private const string RecentSql = """
        SELECT br."Id" AS BorrowRecordId, br."MemberId", br."BookCopyId",
               COALESCE(m."Name", '') AS MemberName, COALESCE(b."Title", '') AS BookTitle,
               br."BorrowedAt", br."DueAt", br."Status"
        FROM borrow_records br
        LEFT JOIN members m ON m."Id" = br."MemberId"
        LEFT JOIN book_copies bc ON bc."Id" = br."BookCopyId"
        LEFT JOIN books b ON b."Id" = bc."BookId"
        WHERE br."IsDeleted" = FALSE
        ORDER BY br."BorrowedAt" DESC
        LIMIT 5
        """;

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        using (QueryCallerScope.Enter(nameof(GetSnapshotAsync)))
        await using (var connection = await connectionFactory.OpenAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            var agg = await connection.QuerySingleAsync<Aggregates>(
                new CommandDefinition(AggregateSql, new { now, soon = now.AddDays(30) }, cancellationToken: cancellationToken));

            var recent = (await connection.QueryAsync<RecentRow>(
                    new CommandDefinition(RecentSql, cancellationToken: cancellationToken)))
                .Select(r => new RecentBorrowActivity(
                    Guid.Parse(r.BorrowRecordId), Guid.Parse(r.MemberId), Guid.Parse(r.BookCopyId),
                    r.MemberName, r.BookTitle, r.BorrowedAt, r.DueAt, Enum.Parse<Domain.Enums.BorrowStatus>(r.Status)))
                .ToList();

            return new DashboardSnapshot(
                (int)agg.TotalBooks, (int)agg.TotalCopies, (int)agg.AvailableCopies, (int)agg.BorrowedCopies, (int)agg.OutOfServiceCopies,
                (int)agg.TotalMembers, (int)agg.ActiveMembers, (int)agg.SuspendedMembers, (int)agg.InactiveMembers, (int)agg.MembersExpiringSoon,
                (int)agg.ActiveBorrows, (int)agg.OverdueBorrows, recent);
        }
    }

    // Plain classes (settable props) so Dapper tolerates SQLite's Int64 COUNT(*).
    private sealed class Aggregates
    {
        public long TotalBooks { get; init; }
        public long TotalCopies { get; init; }
        public long AvailableCopies { get; init; }
        public long BorrowedCopies { get; init; }
        public long OutOfServiceCopies { get; init; }
        public long TotalMembers { get; init; }
        public long ActiveMembers { get; init; }
        public long SuspendedMembers { get; init; }
        public long InactiveMembers { get; init; }
        public long MembersExpiringSoon { get; init; }
        public long ActiveBorrows { get; init; }
        public long OverdueBorrows { get; init; }
    }

    private sealed class RecentRow
    {
        // GUIDs come back as string under SQLite, as Guid under Npgsql - keep as
        // string and parse so the store is provider-agnostic.
        public string BorrowRecordId { get; init; } = string.Empty;
        public string MemberId { get; init; } = string.Empty;
        public string BookCopyId { get; init; } = string.Empty;
        public string MemberName { get; init; } = string.Empty;
        public string BookTitle { get; init; } = string.Empty;
        public DateTime BorrowedAt { get; init; }
        public DateTime DueAt { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
