using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryBorrowRequestRepository : IBorrowRequestRepository
{
    private readonly List<BorrowRequest> _requests = [];

    public Task<BorrowRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_requests.FirstOrDefault(x => x.Id == id));

    public IQueryable<BorrowRequest> Query() => _requests.AsQueryable();

    public Task<IReadOnlyList<BorrowRequest>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BorrowRequest> result = [.. _requests.Where(x => x.MemberId == memberId)];
        return Task.FromResult(result);
    }

    public Task<bool> HasPendingBorrowRequestAsync(Guid memberId, Guid bookId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_requests.Any(x =>
            x.MemberId == memberId && x.BookId == bookId
            && x.Type == BorrowRequestType.Borrow && x.Status == BorrowRequestStatus.Pending));

    public Task AddAsync(BorrowRequest request, CancellationToken cancellationToken = default)
    {
        _requests.Add(request);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BorrowRequest request, CancellationToken cancellationToken = default)
    {
        var index = _requests.FindIndex(x => x.Id == request.Id);
        if (index >= 0)
        {
            _requests[index] = request;
        }

        return Task.CompletedTask;
    }
}
