namespace Library.Domain.Enums;

/// <summary>
/// Lifecycle state of a library member. Append only - never reorder or
/// remove a value (persisted as its string name).
/// </summary>
public enum MemberStatus
{
    /// <summary>In good standing and allowed to borrow.</summary>
    Active,

    /// <summary>Temporarily blocked (e.g. an overdue borrow). Reversible via reactivate/renew.</summary>
    Suspended,

    /// <summary>Membership has lapsed (expired). Must renew to borrow again.</summary>
    Inactive
}
