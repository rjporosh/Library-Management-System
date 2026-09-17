using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure.Persistence.Repositories.EfCore;

public sealed class EfUserRepository(LibraryDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(
            u => u.Username == usernameOrEmail || u.Email == usernameOrEmail, cancellationToken);

    public Task<User?> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.MemberId == memberId, cancellationToken);

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Username == username, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await db.Users.AddAsync(user, cancellationToken);

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        db.Users.Update(user);
        return Task.CompletedTask;
    }
}
