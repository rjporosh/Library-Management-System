using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IBorrowRequestRepository
{
    Task<BorrowRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Composable query root for the generic advanced-search builder.</summary>
    IQueryable<BorrowRequest> Query();

    Task<IReadOnlyList<BorrowRequest>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<bool> HasPendingBorrowRequestAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default);

    Task AddAsync(BorrowRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(BorrowRequest request, CancellationToken cancellationToken = default);
}
