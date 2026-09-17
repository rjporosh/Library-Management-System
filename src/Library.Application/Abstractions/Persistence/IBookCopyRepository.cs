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

    /// <summary>
    /// Highest numeric suffix currently used by a barcode starting with
    /// <paramref name="prefix"/> (e.g. "BC-"), or 0 if none exist. Used to
    /// continue a sequential barcode series (BC-0001, BC-0002, ...) rather
    /// than restart it when auto-generating copies for a new book.
    /// </summary>
    Task<int> GetMaxBarcodeNumberAsync(
        string prefix,
        CancellationToken cancellationToken = default);
}
