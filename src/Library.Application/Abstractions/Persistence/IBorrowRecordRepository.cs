using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBorrowRecordRepository
{
    Task<BorrowRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Composable query root for the generic advanced-search builder.</summary>
    IQueryable<BorrowRecord> Query();

    Task<IReadOnlyList<BorrowRecord>> GetByMemberIdAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BorrowRecord>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        BorrowRecord record,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BorrowRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the member already has an active (not yet returned)
    /// borrow. Enforces the "one active book per member" business rule.
    /// </summary>
    Task<bool> HasActiveBorrowAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the given book copy already has an active borrow.
    /// </summary>
    Task<bool> HasActiveBorrowForCopyAsync(
        Guid bookCopyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every active borrow whose due date has passed as of
    /// <paramref name="asOfUtc"/>. Used by the nightly maintenance job.
    /// </summary>
    Task<IReadOnlyList<BorrowRecord>> GetOverdueActiveAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}
