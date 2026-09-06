using Library.Application.Common.Options;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Dapper;
using Library.Infrastructure.Persistence.Repositories.EfCore;
using Library.Infrastructure.Persistence.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Library.IntegrationTests.Persistence;

/// <summary>
/// The Dapper dashboard read store must produce the same numbers as the EF
/// one. Runs both against a temp-file SQLite database.
/// </summary>
public sealed class DapperDashboardParityTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"lms-dapper-{Guid.NewGuid():N}.db");
    private readonly string _connectionString;
    private readonly LibraryDbContext _db;

    public DapperDashboardParityTests()
    {
        _connectionString = $"Data Source={_dbPath}";

        _db = new LibraryDbContext(new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(_connectionString)
            .Options);
        _db.Database.EnsureCreated();

        var data = SeedData.Build();
        _db.Books.AddRange(data.Books);
        _db.BookCopies.AddRange(data.Copies);
        _db.Members.AddRange(data.Members);
        _db.BorrowRecords.AddRange(data.Borrows);
        _db.SaveChanges();
    }

    [Fact]
    public async Task DapperAndEf_ProduceIdenticalAggregates()
    {
        var options = new DatabaseOptions { Provider = "Sqlite", ConnectionString = _connectionString };
        var dapper = new DapperDashboardReadStore(new DbConnectionFactory(options));
        var ef = new EfDashboardReadStore(_db);

        var d = await dapper.GetSnapshotAsync();
        var e = await ef.GetSnapshotAsync();

        Assert.Equal(e.TotalBooks, d.TotalBooks);
        Assert.Equal(e.TotalCopies, d.TotalCopies);
        Assert.Equal(e.AvailableCopies, d.AvailableCopies);
        Assert.Equal(e.BorrowedCopies, d.BorrowedCopies);
        Assert.Equal(e.OutOfServiceCopies, d.OutOfServiceCopies);
        Assert.Equal(e.TotalMembers, d.TotalMembers);
        Assert.Equal(e.ActiveMembers, d.ActiveMembers);
        Assert.Equal(e.InactiveMembers, d.InactiveMembers);
        Assert.Equal(e.ActiveBorrows, d.ActiveBorrows);
        Assert.Equal(e.OverdueBorrows, d.OverdueBorrows);
        Assert.Equal(e.RecentActivity.Count, d.RecentActivity.Count);
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
