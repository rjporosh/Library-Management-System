using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Member member,
        CancellationToken cancellationToken = default);
}