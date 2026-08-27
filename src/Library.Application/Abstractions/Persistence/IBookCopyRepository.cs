using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBookCopyRepository
{
    Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(
        Guid bookId,
        CancellationToken cancellationToken = default);

    Task<BookCopy?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default);
}