using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.Persistence;

/// <summary>
/// Brings a relational database up to the current schema on startup.
///
/// Beyond a plain <c>MigrateAsync()</c> this also handles the common
/// "the tables exist but <c>__EFMigrationsHistory</c> is empty" case - a
/// database created from <c>docs/database/schema.sql</c>, an older
/// <c>EnsureCreated()</c> run, or a restored dump. Instead of failing with
/// <c>42P07 relation "books" already exists</c>, the existing schema is
/// <b>adopted</b>: every migration in the assembly is recorded as applied, so
/// subsequent runs migrate normally.
/// </summary>
public static class DatabaseBootstrapper
{
    public static async Task MigrateAsync(
        LibraryDbContext db,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var history = db.GetService<IHistoryRepository>();

        // GetPendingMigrations/GetAppliedMigrations read __EFMigrationsHistory;
        // ensure it exists first (idempotent) so an EnsureCreated-style database
        // with no history table does not fail with "relation does not exist".
        await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        var creator = db.GetService<IRelationalDatabaseCreator>();

        if (applied.Count == 0 && await creator.HasTablesAsync(cancellationToken))
        {
            logger?.LogWarning(
                "Database already contains tables but no migration history. Adopting the existing schema " +
                "and baselining {Count} migration(s): {Migrations}.",
                pending.Count, string.Join(", ", pending));

            var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "10.0.0";
            foreach (var migrationId in pending)
            {
                var insert = history.GetInsertScript(new HistoryRow(migrationId, productVersion));
                await db.Database.ExecuteSqlRawAsync(insert, cancellationToken);
            }

            pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
            {
                return;
            }
        }

        await db.Database.MigrateAsync(cancellationToken);
    }
}
