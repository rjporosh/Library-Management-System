using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.UnitTests.Common;

/// <summary>
/// In-memory <see cref="IBookRepository"/> for service tests. Seed books via
/// the constructor; by default any id is treated as an existing book so
/// book-copy tests that are not exercising the FK check stay simple.
/// </summary>
public sealed class StubBookRepository(IEnumerable<Book>? books = null, bool treatAllIdsAsExisting = true)
    : IBookRepository
{
    public List<Book> Books { get; } = books?.ToList() ?? [];

    public Task<(IReadOnlyList<Book> Items, int TotalItems)> GetAsync(BookQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Book> items = [.. Books];
        return Task.FromResult((items, Books.Count));
    }

    public IQueryable<Book> Query() => Books.AsQueryable();

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = Books.FirstOrDefault(x => x.Id == id);
        if (book is null && treatAllIdsAsExisting)
        {
            book = new Book(id, "0000000000", "Seed", "Seed", 2000);
        }

        return Task.FromResult(book);
    }

    public Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default) =>
        Task.FromResult(Books.FirstOrDefault(x => string.Equals(x.ISBN, isbn, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByIsbnAsync(string isbn, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Books.Any(x =>
            string.Equals(x.ISBN, isbn, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || x.Id != excludingId)));

    public Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        Books.Add(book);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Book> books, CancellationToken cancellationToken = default)
    {
        Books.AddRange(books);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        var index = Books.FindIndex(x => x.Id == book.Id);
        if (index >= 0)
        {
            Books[index] = book;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Book book, CancellationToken cancellationToken = default)
    {
        Books.RemoveAll(x => x.Id == book.Id);
        return Task.CompletedTask;
    }
}
