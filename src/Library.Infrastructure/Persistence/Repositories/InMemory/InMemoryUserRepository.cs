using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(x => x.Id == id));

    public Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(x =>
            string.Equals(x.Username, usernameOrEmail, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Email, usernameOrEmail, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(x => x.MemberId == memberId));

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Any(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Any(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(x => x.Id == user.Id);
        if (index >= 0)
        {
            _users[index] = user;
        }

        return Task.CompletedTask;
    }

    public void Seed(IEnumerable<User> users) => _users.AddRange(users);
}
