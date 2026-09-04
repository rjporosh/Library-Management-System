using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;
using Library.Domain.Enums;

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

    public IQueryable<BorrowRecord> Query() => _records.AsQueryable();

    public Task<IReadOnlyList<BorrowRecord>> GetByMemberIdAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> records =
            [.. _records.Where(x => x.MemberId == memberId).OrderByDescending(x => x.BorrowedAt)];

        return Task.FromResult(records);
    }

    public Task<IReadOnlyList<BorrowRecord>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> records = [.. _records];

        return Task.FromResult(records);
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
        var index = _records.FindIndex(x => x.Id == record.Id);
        if (index >= 0)
        {
            _records[index] = record;
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasActiveBorrowAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var hasActive = _records.Any(x =>
            x.MemberId == memberId &&
            x.Status == BorrowStatus.Active);

        return Task.FromResult(hasActive);
    }

    public Task<bool> HasActiveBorrowForCopyAsync(
        Guid bookCopyId,
        CancellationToken cancellationToken = default)
    {
        var hasActive = _records.Any(x =>
            x.BookCopyId == bookCopyId &&
            x.Status == BorrowStatus.Active);

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

    public void Seed(IEnumerable<BorrowRecord> records)
    {
        _records.AddRange(records);
    }
}
