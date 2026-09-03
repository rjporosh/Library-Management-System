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

    /// <summary>
    /// Persists changes made to an already-tracked member (status
    /// transitions such as suspend/reactivate/renew).
    /// </summary>
    Task UpdateAsync(
        Member member,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every member. Used by administrative and background
    /// (cron) processes. Not paginated by design - callers that need
    /// paging/search should use a dedicated query model instead.
    /// </summary>
    Task<IReadOnlyList<Member>> GetAllAsync(
        CancellationToken cancellationToken = default);
}