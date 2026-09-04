using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBookCopyRepository
{
    Task<IReadOnlyList<BookCopy>> GetByBookIdAsync(
        Guid bookId,
        CancellationToken cancellationToken = default);

    /// <summary>Composable query root for the generic advanced-search builder.</summary>
    IQueryable<BookCopy> Query();

    Task<BookCopy?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByBarcodeAsync(
        string barcode,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<BookCopy> bookCopies,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default);
}
