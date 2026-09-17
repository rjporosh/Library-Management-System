using Library.Application.Abstractions.Persistence;
using Library.Application.Common;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryBookCopyRepository : IBookCopyRepository
{
    private readonly List<BookCopy> _copies = [];

    public Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BookCopy> copies = [.. _copies.Where(x => x.BookId == bookId && !x.IsDeleted)];
        return Task.FromResult(copies);
    }

    public IQueryable<BookCopy> Query() => _copies.Where(c => !c.IsDeleted).AsQueryable();

    public Task<BookCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_copies.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_copies.Any(x => !x.IsDeleted
            && string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || x.Id != excludingId)));

    public Task AddAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        _copies.Add(bookCopy);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<BookCopy> bookCopies, CancellationToken cancellationToken = default)
    {
        _copies.AddRange(bookCopies);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        var index = _copies.FindIndex(x => x.Id == bookCopy.Id);
        if (index >= 0)
        {
            _copies[index] = bookCopy;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(BookCopy bookCopy, CancellationToken cancellationToken = default)
    {
        bookCopy.MarkDeleted();
        return Task.CompletedTask;
    }

    public Task<int> GetMaxBarcodeNumberAsync(string prefix, CancellationToken cancellationToken = default) =>
        Task.FromResult(BarcodeSequence.MaxSuffix(prefix, _copies.Select(c => c.Barcode)));

    public void Seed(IEnumerable<BookCopy> copies) => _copies.AddRange(copies);
}
