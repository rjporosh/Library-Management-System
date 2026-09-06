using Library.Domain.Common;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the library. Provider-agnostic model: enums are stored
/// as their string name (matching the API and keeping the DB human-readable),
/// unique/foreign-key constraints back the business rules, audit timestamps
/// and the soft-delete flag are maintained here, and a global query filter
/// hides soft-deleted rows from every normal query.
/// </summary>
public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Book>(e =>
        {
            e.ToTable("books");
            ConfigureEntity(e);
            e.Property(x => x.ISBN).HasMaxLength(20).IsRequired();
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.Property(x => x.Author).HasMaxLength(400).IsRequired();
            e.Property(x => x.Category).HasMaxLength(200).IsRequired();
            e.Property(x => x.Publisher).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasIndex(x => x.ISBN).IsUnique();
            e.HasIndex(x => x.Title);
            e.HasIndex(x => x.Category);
        });

        b.Entity<BookCopy>(e =>
        {
            e.ToTable("book_copies");
            ConfigureEntity(e);
            e.Property(x => x.Barcode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.Barcode).IsUnique();
            e.HasIndex(x => x.BookId);
            e.HasIndex(x => x.Status);
            e.HasOne<Book>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Member>(e =>
        {
            e.ToTable("members");
            ConfigureEntity(e);
            e.Property(x => x.MembershipNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(64).IsRequired();
            e.Property(x => x.Address).HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.MembershipNumber).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.MembershipExpiresAt);
        });

        b.Entity<BorrowRecord>(e =>
        {
            e.ToTable("borrow_records");
            ConfigureEntity(e);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.MemberId, x.Status });
            e.HasIndex(x => new { x.Status, x.DueAt });
            e.HasOne<BookCopy>().WithMany().HasForeignKey(x => x.BookCopyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Member>().WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAtUtc").CurrentValue = now;
                entry.Property("UpdatedAtUtc").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("UpdatedAtUtc").CurrentValue = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureEntity<T>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e)
        where T : Entity
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.IsDeleted).IsRequired();
        e.HasIndex(x => x.IsDeleted);
        e.Property<DateTime>("CreatedAtUtc");
        e.Property<DateTime>("UpdatedAtUtc");
        e.HasQueryFilter(x => !x.IsDeleted);
    }
}
