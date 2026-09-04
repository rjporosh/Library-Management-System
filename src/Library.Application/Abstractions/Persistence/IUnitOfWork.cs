namespace Library.Application.Abstractions.Persistence;

/// <summary>
/// Commit boundary for a use case. In-memory this is a no-op; under EF Core it
/// flushes the change tracker. Multi-entity operations (issue/return, bulk
/// import) additionally wrap their work in a transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>An open transaction. Dispose without commit rolls back.</summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
