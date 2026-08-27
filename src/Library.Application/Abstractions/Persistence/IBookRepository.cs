using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Book?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Book book,
        CancellationToken cancellationToken = default);
}