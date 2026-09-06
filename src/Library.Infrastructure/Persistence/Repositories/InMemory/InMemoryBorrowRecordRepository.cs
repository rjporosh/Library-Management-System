using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryBorrowRecordRepository : IBorrowRecordRepository
{
    private readonly List<BorrowRecord> _records = [];

    public Task<BorrowRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public IQueryable<BorrowRecord> Query() => _records.Where(r => !r.IsDeleted).AsQueryable();

    public Task<IReadOnlyList<BorrowRecord>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> records =
            [.. _records.Where(x => x.MemberId == memberId && !x.IsDeleted).OrderByDescending(x => x.BorrowedAt)];
        return Task.FromResult(records);
    }

    public Task<IReadOnlyList<BorrowRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> records = [.. _records.Where(r => !r.IsDeleted)];
        return Task.FromResult(records);
    }

    public Task AddAsync(BorrowRecord record, CancellationToken cancellationToken = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BorrowRecord record, CancellationToken cancellationToken = default)
    {
        var index = _records.FindIndex(x => x.Id == record.Id);
        if (index >= 0)
        {
            _records[index] = record;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(BorrowRecord record, CancellationToken cancellationToken = default)
    {
        record.MarkDeleted();
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveBorrowAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.Any(x => x.MemberId == memberId && !x.IsDeleted && x.Status == BorrowStatus.Active));

    public Task<bool> HasActiveBorrowForCopyAsync(Guid bookCopyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.Any(x => x.BookCopyId == bookCopyId && !x.IsDeleted && x.Status == BorrowStatus.Active));

    public Task<IReadOnlyList<BorrowRecord>> GetOverdueActiveAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> overdue = [.. _records.Where(x => !x.IsDeleted && x.IsOverdue(asOfUtc))];
        return Task.FromResult(overdue);
    }

    public void Seed(IEnumerable<BorrowRecord> records) => _records.AddRange(records);
}
