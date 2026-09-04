using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryBookCopyRepository : IBookCopyRepository
{
    private readonly List<BookCopy> _copies = [];

    public Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BookCopy> copies = _copies
            .Where(x => x.BookId == bookId)
            .ToList();

        return Task.FromResult(copies);
    }

    public IQueryable<BookCopy> Query() => _copies.AsQueryable();

    public Task<BookCopy?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var copy = _copies.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(copy);
    }

    public Task<bool> ExistsByBarcodeAsync(
        string barcode,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = _copies.Any(x =>
            string.Equals(x.Barcode, barcode, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || x.Id != excludingId));

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default)
    {
        _copies.Add(bookCopy);

        return Task.CompletedTask;
    }

    public Task AddRangeAsync(
        IEnumerable<BookCopy> bookCopies,
        CancellationToken cancellationToken = default)
    {
        _copies.AddRange(bookCopies);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default)
    {
        var index = _copies.FindIndex(x => x.Id == bookCopy.Id);
        if (index >= 0)
        {
            _copies[index] = bookCopy;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default)
    {
        _copies.RemoveAll(x => x.Id == bookCopy.Id);

        return Task.CompletedTask;
    }

    public void Seed(IEnumerable<BookCopy> copies)
    {
        _copies.AddRange(copies);
    }
}
