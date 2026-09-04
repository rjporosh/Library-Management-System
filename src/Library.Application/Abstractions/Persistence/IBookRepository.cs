using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task<(IReadOnlyList<Book> Items, int TotalItems)> GetAsync(
        BookQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Composable query root for the generic advanced-search builder.</summary>
    IQueryable<Book> Query();

    Task<Book?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Book?> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default);

    /// <summary>True when another book already uses this ISBN (optionally ignoring one id).</summary>
    Task<bool> ExistsByIsbnAsync(
        string isbn,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Book book,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<Book> books,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Book book,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Book book,
        CancellationToken cancellationToken = default);
}
