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
}