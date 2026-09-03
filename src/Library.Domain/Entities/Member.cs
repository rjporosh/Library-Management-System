using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class Member
{
    public Guid Id { get; init; }

    public string MembershipNumber { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public MemberStatus Status { get; private set; }

    /// <summary>
    /// UTC timestamp of the most recent suspension. Null when the
    /// member has never been suspended or has since been reactivated.
    /// </summary>
    public DateTime? SuspendedAt { get; private set; }

    /// <summary>
    /// UTC timestamp of the most recent membership renewal.
    /// </summary>
    public DateTime? LastRenewedAt { get; private set; }

    public Member(
        Guid id,
        string membershipNumber,
        string name,
        string email)
    {
        Id = id;
        MembershipNumber = membershipNumber;
        Name = name;
        Email = email;
        Status = MemberStatus.Active;
    }

    public bool CanBorrow()
    {
        return Status == MemberStatus.Active;
    }

    /// <summary>
    /// Suspends the member (e.g. because of an overdue borrow).
    /// Idempotent: suspending an already-suspended member is a no-op
    /// aside from refreshing the suspension timestamp.
    /// </summary>
    public void Suspend()
    {
        Status = MemberStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the member back to Active without treating it as a
    /// membership renewal (e.g. an administrative override).
    /// </summary>
    public void Reactivate()
    {
        Status = MemberStatus.Active;
        SuspendedAt = null;
    }

    /// <summary>
    /// Renews the member's membership: clears any suspension and
    /// records the renewal timestamp so staff can see when the
    /// member's standing was last refreshed.
    /// </summary>
    public void Renew()
    {
        Status = MemberStatus.Active;
        SuspendedAt = null;
        LastRenewedAt = DateTime.UtcNow;
    }
}