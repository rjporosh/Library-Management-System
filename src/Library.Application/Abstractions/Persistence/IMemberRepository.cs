using Library.Domain.Entities;

namespace Library.Application.Abstractions.Persistence;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Composable query root for the generic advanced-search builder.</summary>
    IQueryable<Member> Query();

    Task<bool> ExistsByMembershipNumberAsync(
        string membershipNumber,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(
        string email,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Member member,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<Member> members,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes made to an already-tracked member (status
    /// transitions such as suspend/reactivate/renew/deactivate).
    /// </summary>
    Task UpdateAsync(
        Member member,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Member member,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every member. Used by administrative and background
    /// (cron) processes.
    /// </summary>
    Task<IReadOnlyList<Member>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
