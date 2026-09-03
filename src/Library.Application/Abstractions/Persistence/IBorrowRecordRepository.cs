using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBorrowRecordRepository
{
    Task<BorrowRecord?> GetByIdAsync(
        Guid id,
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
    /// Returns every active borrow whose due date has passed as of
    /// <paramref name="asOfUtc"/>. Used by the member-suspension cron job.
    /// </summary>
    Task<IReadOnlyList<BorrowRecord>> GetOverdueActiveAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}