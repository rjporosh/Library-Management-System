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

    public Task AddAsync(
        Member member,
        CancellationToken cancellationToken = default)
    {
        _members.Add(member);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Member member,
        CancellationToken cancellationToken = default)
    {
        // In-memory: the tracked instance is the same reference held
        // in _members, so mutations already applied via the entity's
        // methods (Suspend/Reactivate/Renew) are already visible.
        // A real (EF Core/Dapper) repository will persist the change
        // here instead - see ROADMAP Phase 6/11.
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
