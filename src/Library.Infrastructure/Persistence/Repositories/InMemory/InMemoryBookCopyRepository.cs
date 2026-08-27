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

    public Task<BookCopy?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var copy = _copies.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(copy);
    }

    public Task AddAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default)
    {
        _copies.Add(bookCopy);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Seed(IEnumerable<BookCopy> copies)
    {
        _copies.AddRange(copies);
    }
}