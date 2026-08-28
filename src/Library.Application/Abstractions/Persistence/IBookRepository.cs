using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task<(IReadOnlyList<Book> Items, int TotalItems)> GetAsync(
        BookQuery query,
        CancellationToken cancellationToken = default);

    Task<Book?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Book book,
        CancellationToken cancellationToken = default);
}