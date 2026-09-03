using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryBorrowRecordRepository : IBorrowRecordRepository
{
    private readonly List<BorrowRecord> _records = [];

    public Task<BorrowRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var record = _records.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(record);
    }

    public Task AddAsync(
        BorrowRecord record,
        CancellationToken cancellationToken = default)
    {
        _records.Add(record);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        BorrowRecord record,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveBorrowAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var hasActive = _records.Any(x =>
            x.MemberId == memberId &&
            x.Status == Domain.Enums.BorrowStatus.Active);

        return Task.FromResult(hasActive);
    }

    public Task<IReadOnlyList<BorrowRecord>> GetOverdueActiveAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> overdue =
            [.. _records.Where(x => x.IsOverdue(asOfUtc))];

        return Task.FromResult(overdue);
    }
}
