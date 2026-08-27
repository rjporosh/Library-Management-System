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

    public void Seed(IEnumerable<Member> members)
    {
        _members.AddRange(members);
    }
}
