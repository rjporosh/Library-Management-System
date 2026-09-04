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

    /// <summary>
    /// UTC date the membership lapses. When this passes, the nightly
    /// maintenance job moves the member to <see cref="MemberStatus.Inactive"/>.
    /// </summary>
    public DateTime MembershipExpiresAt { get; private set; }

    /// <summary>Default membership term applied on creation and renewal.</summary>
    public const int MembershipTermDays = 365;

    public Member(
        Guid id,
        string membershipNumber,
        string name,
        string email,
        DateTime? membershipExpiresAt = null)
    {
        Id = id;
        MembershipNumber = membershipNumber;
        Name = name;
        Email = email;
        Status = MemberStatus.Active;
        MembershipExpiresAt = membershipExpiresAt
            ?? DateTime.UtcNow.Date.AddDays(MembershipTermDays);
    }

    /// <summary>True when the membership term has elapsed as of <paramref name="asOfUtc"/>.</summary>
    public bool IsExpired(DateTime asOfUtc) => MembershipExpiresAt < asOfUtc;

    /// <summary>Updates the editable profile fields. Does not touch status or expiry.</summary>
    public void UpdateProfile(string membershipNumber, string name, string email)
    {
        MembershipNumber = membershipNumber;
        Name = name;
        Email = email;
    }

    public bool CanBorrow()
    {
        return Status == MemberStatus.Active && !IsExpired(DateTime.UtcNow);
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
    /// Marks the membership lapsed. Used by the nightly maintenance job
    /// once <see cref="MembershipExpiresAt"/> has passed. Leaves a
    /// suspended member suspended - only an active member goes inactive.
    /// </summary>
    public void Deactivate()
    {
        if (Status == MemberStatus.Active)
        {
            Status = MemberStatus.Inactive;
        }
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

        // Extend the term from whichever is later - today or the current
        // expiry - so an early renewal is not penalised and a lapsed one
        // still gets a full fresh term.
        var basis = MembershipExpiresAt > DateTime.UtcNow
            ? MembershipExpiresAt
            : DateTime.UtcNow.Date;
        MembershipExpiresAt = basis.AddDays(MembershipTermDays);
    }
}