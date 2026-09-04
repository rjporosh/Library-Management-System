using Library.Application.Common.Options;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

/// <summary>
/// Maps <see cref="DatabaseOptions"/> onto the matching EF Core provider.
/// Adding a new relational provider is a single case here plus a NuGet
/// reference - no other code changes (spec §2, §14).
/// </summary>
public static class DatabaseProviderConfigurator
{
    public static void Configure(DbContextOptionsBuilder builder, DatabaseOptions options)
    {
        var cs = options.ConnectionString;

        switch (options.ResolvedProvider)
        {
            case DatabaseProvider.Postgres:
                builder.UseNpgsql(Require(cs, "Postgres"));
                break;

            case DatabaseProvider.SqlServer:
                builder.UseSqlServer(Require(cs, "SqlServer"));
                break;

            case DatabaseProvider.Sqlite:
                builder.UseSqlite(Require(cs, "Sqlite"));
                break;

            case DatabaseProvider.MySql:
            case DatabaseProvider.Oracle:
            case DatabaseProvider.Access:
            case DatabaseProvider.Mongo:
                throw new NotSupportedException(
                    $"Database provider '{options.Provider}' is an acknowledged configuration slot but has no " +
                    "implementation yet (no EF Core 10 driver, or not a relational database). " +
                    "Use Postgres (primary), SqlServer, Sqlite or InMemory. See docs/programmers-guide.");

            default:
                throw new NotSupportedException(
                    $"Database provider '{options.Provider}' is not recognised.");
        }
    }

    private static string Require(string? connectionString, string provider) =>
        !string.IsNullOrWhiteSpace(connectionString)
            ? connectionString
            : throw new InvalidOperationException(
                $"Database:ConnectionString is required when Database:Provider is '{provider}'.");
}
