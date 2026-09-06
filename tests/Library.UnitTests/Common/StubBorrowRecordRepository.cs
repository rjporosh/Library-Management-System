using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.UnitTests.Common;

/// <summary>
/// Minimal in-memory <see cref="IBorrowRecordRepository"/> for service tests
/// that need the dependency but do not exercise borrow behaviour.
/// </summary>
public sealed class StubBorrowRecordRepository : IBorrowRecordRepository
{
    public List<BorrowRecord> Records { get; } = [];

    public Task<BorrowRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.FirstOrDefault(x => x.Id == id));

    public IQueryable<BorrowRecord> Query() => Records.AsQueryable();

    public Task<IReadOnlyList<BorrowRecord>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> r = [.. Records.Where(x => x.MemberId == memberId)];
        return Task.FromResult(r);
    }

    public Task<IReadOnlyList<BorrowRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> r = [.. Records];
        return Task.FromResult(r);
    }

    public Task AddAsync(BorrowRecord record, CancellationToken cancellationToken = default)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BorrowRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(BorrowRecord record, CancellationToken cancellationToken = default)
    {
        record.MarkDeleted();
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveBorrowAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.Any(x => x.MemberId == memberId && x.Status == BorrowStatus.Active));

    public Task<bool> HasActiveBorrowForCopyAsync(Guid bookCopyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.Any(x => x.BookCopyId == bookCopyId && x.Status == BorrowStatus.Active));

    public Task<IReadOnlyList<BorrowRecord>> GetOverdueActiveAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRecord> r = [.. Records.Where(x => x.IsOverdue(asOfUtc))];
        return Task.FromResult(r);
    }
}
