using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Seed;

/// <summary>
/// Loads the canonical <see cref="SeedData"/> dataset into a relational
/// database. Idempotent - does nothing if books already exist.
/// </summary>
public sealed class DatabaseSeeder(LibraryDbContext db)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Books.AnyAsync(cancellationToken))
        {
            return;
        }

        var data = SeedData.Build();
        db.Books.AddRange(data.Books);
        db.BookCopies.AddRange(data.Copies);
        db.Members.AddRange(data.Members);
        db.BorrowRecords.AddRange(data.Borrows);

        await db.SaveChangesAsync(cancellationToken);
    }
}
