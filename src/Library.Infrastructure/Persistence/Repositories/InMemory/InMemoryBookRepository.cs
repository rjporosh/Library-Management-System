using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly List<Book> _books = [];

    public Task<IReadOnlyList<Book>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Book> books = _books.ToList();

        return Task.FromResult(books);
    }

    public Task<Book?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var book = _books.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(book);
    }

    public Task AddAsync(
        Book book,
        CancellationToken cancellationToken = default)
    {
        _books.Add(book);

        return Task.CompletedTask;
    }

    public void Seed(IEnumerable<Book> books)
    {
        _books.AddRange(books);
    }
}
