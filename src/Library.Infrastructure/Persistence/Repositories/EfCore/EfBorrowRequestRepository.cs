using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories.EfCore;

public sealed class EfBorrowRequestRepository(LibraryDbContext db) : IBorrowRequestRepository
{
    public Task<BorrowRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BorrowRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public IQueryable<BorrowRequest> Query() => db.BorrowRequests.AsNoTracking();

    public async Task<IReadOnlyList<BorrowRequest>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await db.BorrowRequests.AsNoTracking().Where(r => r.MemberId == memberId).ToListAsync(cancellationToken);

    public Task<bool> HasPendingBorrowRequestAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default) =>
        db.BorrowRequests.AnyAsync(
            r => r.MemberId == memberId && r.BookId == bookId
                && r.Type == BorrowRequestType.Borrow && r.Status == BorrowRequestStatus.Pending,
            cancellationToken);

    public async Task AddAsync(BorrowRequest request, CancellationToken cancellationToken = default) =>
        await db.BorrowRequests.AddAsync(request, cancellationToken);

    public Task UpdateAsync(BorrowRequest request, CancellationToken cancellationToken = default)
    {
        db.BorrowRequests.Update(request);
        return Task.CompletedTask;
    }
}
