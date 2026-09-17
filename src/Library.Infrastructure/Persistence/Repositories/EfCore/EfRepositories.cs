using Library.Application.Abstractions.Persistence;
using Library.Application.Common;
using Library.Application.Common.Pagination;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories.EfCore;

public sealed class EfBookRepository(LibraryDbContext db) : IBookRepository
{
    public async Task<(IReadOnlyList<Book> Items, int TotalItems)> GetAsync(BookQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Book> q = db.Books.AsNoTracking();
        var search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var fields = (query.SearchBy ?? "Title")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(f => f.ToLowerInvariant())
                .ToHashSet();
            if (fields.Count == 0) fields.Add("title");

            var term = search.ToLower();
            q = q.Where(b =>
                (fields.Contains("title") && b.Title.ToLower().Contains(term)) ||
                (fields.Contains("author") && b.Author.ToLower().Contains(term)) ||
                (fields.Contains("isbn") && b.ISBN.ToLower().Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken);

        var desc = string.Equals(query.SortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
        var sortBy = (query.SortBy ?? "Title").Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim().ToLowerInvariant();
        q = (sortBy, desc) switch
        {
            ("author", false) => q.OrderBy(b => b.Author),
            ("author", true) => q.OrderByDescending(b => b.Author),
            ("isbn", false) => q.OrderBy(b => b.ISBN),
            ("isbn", true) => q.OrderByDescending(b => b.ISBN),
            ("publishedyear", false) => q.OrderBy(b => b.PublishedYear),
            ("publishedyear", true) => q.OrderByDescending(b => b.PublishedYear),
            (_, true) => q.OrderByDescending(b => b.Title),
            _ => q.OrderBy(b => b.Title),
        };

        var items = await q.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public IQueryable<Book> Query() => db.Books.AsNoTracking();

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default) =>
        db.Books.FirstOrDefaultAsync(b => b.ISBN == isbn, cancellationToken);

    public Task<bool> ExistsByIsbnAsync(string isbn, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        db.Books.AnyAsync(b => b.ISBN == isbn && (excludingId == null || b.Id != excludingId), cancellationToken);

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default) =>
        await db.Books.AddAsync(book, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Book> books, CancellationToken cancellationToken = default) =>
        await db.Books.AddRangeAsync(books, cancellationToken);

    public Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        db.Books.Update(book);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Book book, CancellationToken cancellationToken = default)
    {
        book.MarkDeleted();
        db.Books.Update(book);
        return Task.CompletedTask;
    }
}

public sealed class EfBookCopyRepository(LibraryDbContext db) : IBookCopyRepository
{
    public async Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
        await db.BookCopies.AsNoTracking().Where(c => c.BookId == bookId).ToListAsync(cancellationToken);

    public IQueryable<BookCopy> Query() => db.BookCopies.AsNoTracking();

    public Task<BookCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BookCopies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        db.BookCopies.AnyAsync(c => c.Barcode == barcode && (excludingId == null || c.Id != excludingId), cancellationToken);

    public async Task AddAsync(BookCopy bookCopy, CancellationToken cancellationToken = default) =>
        await db.BookCopies.AddAsync(bookCopy, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<BookCopy> bookCopies, CancellationToken cancellationToken = default) =>
        await db.BookCopies.AddRangeAsync(bookCopies, cancellationToken);

    public Task UpdateAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        db.BookCopies.Update(bookCopy);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        bookCopy.MarkDeleted();
        db.BookCopies.Update(bookCopy);
        return Task.CompletedTask;
    }

    public async Task<int> GetMaxBarcodeNumberAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var barcodes = await db.BookCopies.AsNoTracking()
            .Where(c => c.Barcode.StartsWith(prefix))
            .Select(c => c.Barcode)
            .ToListAsync(cancellationToken);
        return BarcodeSequence.MaxSuffix(prefix, barcodes);
    }
}

public sealed class EfMemberRepository(LibraryDbContext db) : IMemberRepository
{
    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Members.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public IQueryable<Member> Query() => db.Members.AsNoTracking();

    public Task<bool> ExistsByMembershipNumberAsync(string membershipNumber, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        db.Members.AnyAsync(m => m.MembershipNumber == membershipNumber && (excludingId == null || m.Id != excludingId), cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        db.Members.AnyAsync(m => m.Email == email && (excludingId == null || m.Id != excludingId), cancellationToken);

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default) =>
        await db.Members.AddAsync(member, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Member> members, CancellationToken cancellationToken = default) =>
        await db.Members.AddRangeAsync(members, cancellationToken);

    public Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
    {
        db.Members.Update(member);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Member member, CancellationToken cancellationToken = default)
    {
        member.MarkDeleted();
        db.Members.Update(member);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Member>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Members.AsNoTracking().ToListAsync(cancellationToken);
}

public sealed class EfBorrowRecordRepository(LibraryDbContext db) : IBorrowRecordRepository
{
    public Task<BorrowRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BorrowRecords.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public IQueryable<BorrowRecord> Query() => db.BorrowRecords.AsNoTracking();

    public async Task<IReadOnlyList<BorrowRecord>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await db.BorrowRecords.AsNoTracking().Where(r => r.MemberId == memberId)
            .OrderByDescending(r => r.BorrowedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BorrowRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.BorrowRecords.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(BorrowRecord record, CancellationToken cancellationToken = default) =>
        await db.BorrowRecords.AddAsync(record, cancellationToken);

    public Task UpdateAsync(BorrowRecord record, CancellationToken cancellationToken = default)
    {
        db.BorrowRecords.Update(record);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BorrowRecord record, CancellationToken cancellationToken = default)
    {
        record.MarkDeleted();
        db.BorrowRecords.Update(record);
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveBorrowAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        db.BorrowRecords.AnyAsync(r => r.MemberId == memberId && r.Status == BorrowStatus.Active, cancellationToken);

    public Task<bool> HasActiveBorrowForCopyAsync(Guid bookCopyId, CancellationToken cancellationToken = default) =>
        db.BorrowRecords.AnyAsync(r => r.BookCopyId == bookCopyId && r.Status == BorrowStatus.Active, cancellationToken);

    public async Task<IReadOnlyList<BorrowRecord>> GetOverdueActiveAsync(DateTime asOfUtc, CancellationToken cancellationToken = default) =>
        await db.BorrowRecords.AsNoTracking()
            .Where(r => r.Status == BorrowStatus.Active && r.DueAt < asOfUtc).ToListAsync(cancellationToken);
}
