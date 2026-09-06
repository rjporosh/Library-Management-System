using Library.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Library.IntegrationTests.Persistence;

/// <summary>
/// <see cref="DatabaseBootstrapper"/> must bring a database to the current
/// schema in both situations: a genuinely empty database, and one whose tables
/// already exist but whose EF migration history is empty (schema.sql,
/// EnsureCreated, a restored dump). The second case previously crashed the API
/// with <c>relation "books" already exists</c>.
/// </summary>
public sealed class DatabaseBootstrapperTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lms-boot-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;

    public DatabaseBootstrapperTests() => _connectionString = $"Data Source={_dbPath}";

    // The migrations are authored for Npgsql; running them on SQLite for this
    // control-flow test raises a benign model-difference warning we ignore.
    private LibraryDbContext NewContext() => new(new DbContextOptionsBuilder<LibraryDbContext>()
        .UseSqlite(_connectionString)
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
        .Options);

    [Fact]
    public async Task EmptyDatabase_AppliesMigrations()
    {
        await using var db = NewContext();

        await DatabaseBootstrapper.MigrateAsync(db);

        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task SchemaExistsButHistoryEmpty_AdoptsInsteadOfFailing()
    {
        await using (var seed = NewContext())
        {
            // Tables, but no __EFMigrationsHistory rows - the state that used to crash startup.
            await seed.Database.EnsureCreatedAsync();
        }

        await using var db = NewContext();
        var ex = await Record.ExceptionAsync(() => DatabaseBootstrapper.MigrateAsync(db));

        Assert.Null(ex);
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
