using Library.Application.Abstractions.Persistence;
using Library.Application.Common;
using Library.Domain.Entities;

namespace Library.UnitTests.Common;

/// <summary>In-memory <see cref="IBookCopyRepository"/> for service tests. Seed copies via the constructor.</summary>
public sealed class StubBookCopyRepository(IEnumerable<BookCopy>? copies = null) : IBookCopyRepository
{
    public List<BookCopy> Copies { get; } = copies?.ToList() ?? [];

    public Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BookCopy> r = [.. Copies.Where(c => c.BookId == bookId && !c.IsDeleted)];
        return Task.FromResult(r);
    }

    public IQueryable<BookCopy> Query() => Copies.Where(c => !c.IsDeleted).AsQueryable();

    public Task<BookCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Copies.FirstOrDefault(c => c.Id == id && !c.IsDeleted));

    public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Copies.Any(c => !c.IsDeleted
            && string.Equals(c.Barcode, barcode, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || c.Id != excludingId)));

    public Task AddAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        Copies.Add(bookCopy);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<BookCopy> bookCopies, CancellationToken cancellationToken = default)
    {
        Copies.AddRange(bookCopies);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        var i = Copies.FindIndex(c => c.Id == bookCopy.Id);
        if (i >= 0) Copies[i] = bookCopy;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        bookCopy.MarkDeleted();
        return Task.CompletedTask;
    }

    public Task<int> GetMaxBarcodeNumberAsync(string prefix, CancellationToken cancellationToken = default) =>
        Task.FromResult(BarcodeSequence.MaxSuffix(prefix, Copies.Select(c => c.Barcode)));
}
