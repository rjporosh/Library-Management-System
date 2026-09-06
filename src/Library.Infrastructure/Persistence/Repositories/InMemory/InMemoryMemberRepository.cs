using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryMemberRepository : IMemberRepository
{
    private readonly List<Member> _members = [];

    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_members.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public IQueryable<Member> Query() => _members.Where(m => !m.IsDeleted).AsQueryable();

    public Task<bool> ExistsByMembershipNumberAsync(string membershipNumber, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_members.Any(x => !x.IsDeleted
            && string.Equals(x.MembershipNumber, membershipNumber, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || x.Id != excludingId)));

    public Task<bool> ExistsByEmailAsync(string email, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(_members.Any(x => !x.IsDeleted
            && string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || x.Id != excludingId)));

    public Task AddAsync(Member member, CancellationToken cancellationToken = default)
    {
        _members.Add(member);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Member> members, CancellationToken cancellationToken = default)
    {
        _members.AddRange(members);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
    {
        var index = _members.FindIndex(x => x.Id == member.Id);
        if (index >= 0)
        {
            _members[index] = member;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Member member, CancellationToken cancellationToken = default)
    {
        member.MarkDeleted();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Member>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Member> members = [.. _members.Where(m => !m.IsDeleted)];
        return Task.FromResult(members);
    }

    public void Seed(IEnumerable<Member> members) => _members.AddRange(members);
}
