using Library.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Library.Infrastructure.Persistence;

public sealed class EfUnitOfWork(LibraryDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await context.Database.BeginTransactionAsync(cancellationToken));
}

public sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}

/// <summary>No-op unit of work for the in-memory provider (repositories mutate the shared list directly).</summary>
public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ITransaction>(new NoOpTransaction());

    private sealed class NoOpTransaction : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
