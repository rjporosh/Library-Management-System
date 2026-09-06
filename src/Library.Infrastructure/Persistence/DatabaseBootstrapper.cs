using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.Persistence;

/// <summary>
/// Brings a relational database up to the current schema on startup.
///
/// Beyond a plain <c>MigrateAsync()</c> this also handles the common
/// "the tables already exist but <c>__EFMigrationsHistory</c> is empty or
/// stale" case - a database created from <c>docs/database/schema.sql</c>, an
/// old <c>EnsureCreated()</c>, or a restored dump. Instead of failing with
/// <c>42P07 relation "books" already exists</c>, the existing schema is
/// <b>adopted</b>: the missing migrations are recorded in the history table so
/// subsequent runs migrate normally.
/// </summary>
public static class DatabaseBootstrapper
{
    public static async Task MigrateAsync(
        LibraryDbContext db,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        try
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex) when (IsObjectAlreadyExists(ex))
        {
            // The schema is already there - EF just has no record of it.
            // Record the migrations as applied, then run anything genuinely new.
            logger?.LogWarning(ex,
                "Schema already exists but is not tracked by EF. Adopting it and baselining " +
                "{Count} migration(s): {Migrations}.", pending.Count, string.Join(", ", pending));

            await BaselineAsync(db, cancellationToken);

            if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
        }
    }

    private static async Task BaselineAsync(LibraryDbContext db, CancellationToken cancellationToken)
    {
        var history = db.GetService<IHistoryRepository>();
        if (!await history.ExistsAsync(cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
        }

        var alreadyApplied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToHashSet();
        var version = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "10.0.0";

        foreach (var migrationId in db.Database.GetMigrations())
        {
            if (alreadyApplied.Contains(migrationId))
            {
                continue;
            }

            var insert = history.GetInsertScript(new HistoryRow(migrationId, version));
            await db.Database.ExecuteSqlRawAsync(insert, cancellationToken);
        }
    }

    private static bool IsObjectAlreadyExists(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            var sqlState = e.GetType().GetProperty("SqlState")?.GetValue(e) as string;
            if (sqlState is "42P07" or "42710")
            {
                return true;
            }

            if (e.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
