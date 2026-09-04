using Library.Application.Common.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Library.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time only. Migrations are authored
/// against PostgreSQL (the primary provider); the connection string comes from
/// the LMS_DESIGN_CONNECTION environment variable or a localhost default.
/// </summary>
public sealed class LibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args)
    {
        var options = new DatabaseOptions
        {
            Provider = "Postgres",
            ConnectionString =
                Environment.GetEnvironmentVariable("LMS_DESIGN_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=library;Username=library;Password=library",
        };

        var builder = new DbContextOptionsBuilder<LibraryDbContext>();
        DatabaseProviderConfigurator.Configure(builder, options);
        return new LibraryDbContext(builder.Options);
    }
}
