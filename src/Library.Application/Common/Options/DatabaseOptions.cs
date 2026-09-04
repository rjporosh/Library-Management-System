namespace Library.Application.Common.Options;

/// <summary>
/// Bound once at startup from the "Database" section of appsettings. Kept as a
/// plain POCO (same convention as <see cref="ObservabilitySettings"/>) so it is
/// shared as a singleton across layers without an IOptions dependency.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Which persistence provider to use. Switchable by configuration only.
    /// InMemory keeps the zero-dependency demo mode; Postgres is the primary
    /// production provider. SqlServer and Sqlite are also supported by EF Core.
    /// MySql / Oracle / Access / Mongo are acknowledged slots that currently
    /// throw NotSupportedException (no EF Core 10 driver yet / not relational).
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>Which ORM handles read/query paths. Writes always use EF Core when relational.</summary>
    public string Orm { get; set; } = "EfCore";

    public string? ConnectionString { get; set; }

    /// <summary>Apply pending migrations on startup (typically Development only).</summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>Seed demo data on startup when the database is empty.</summary>
    public bool SeedOnStartup { get; set; } = true;

    public DatabaseProvider ResolvedProvider =>
        Enum.TryParse<DatabaseProvider>(Provider, ignoreCase: true, out var p)
            ? p
            : DatabaseProvider.InMemory;

    public bool IsRelational => ResolvedProvider is not (DatabaseProvider.InMemory or DatabaseProvider.Mongo);

    public bool UseDapperReads =>
        string.Equals(Orm, "Dapper", StringComparison.OrdinalIgnoreCase) && IsRelational;
}

public enum DatabaseProvider
{
    InMemory,
    Postgres,
    SqlServer,
    Sqlite,
    MySql,
    Oracle,
    Access,
    Mongo
}
