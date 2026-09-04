using Library.Application.Abstractions.Persistence;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryMemberRepository : IMemberRepository
{
    private readonly List<Member> _members = [];

    public Task<Member?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var member = _members.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(member);
    }

    public IQueryable<Member> Query() => _members.AsQueryable();

    public Task<bool> ExistsByMembershipNumberAsync(
        string membershipNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = _members.Any(x =>
            string.Equals(x.MembershipNumber, membershipNumber, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || x.Id != excludingId));

        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = _members.Any(x =>
            string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || x.Id != excludingId));

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Member member,
        CancellationToken cancellationToken = default)
    {
        _members.Add(member);

        return Task.CompletedTask;
    }

    public Task AddRangeAsync(
        IEnumerable<Member> members,
        CancellationToken cancellationToken = default)
    {
        _members.AddRange(members);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Member member,
        CancellationToken cancellationToken = default)
    {
        // In-memory: the tracked instance is the same reference held in
        // _members, so mutations applied via the entity's methods are
        // already visible. A real (EF Core) repository persists here.
        var index = _members.FindIndex(x => x.Id == member.Id);
        if (index >= 0)
        {
            _members[index] = member;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Member member,
        CancellationToken cancellationToken = default)
    {
        _members.RemoveAll(x => x.Id == member.Id);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Member>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Member> members = [.. _members];

        return Task.FromResult(members);
    }

    public void Seed(IEnumerable<Member> members)
    {
        _members.AddRange(members);
    }
}
