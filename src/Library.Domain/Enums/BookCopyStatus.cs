namespace Library.Domain.Enums;

/// <summary>
/// Physical state of a single book copy. Append only - never reorder or
/// remove a value (persisted as its string name).
/// </summary>
public enum BookCopyStatus
{
    /// <summary>On the shelf, can be issued.</summary>
    Available,

    /// <summary>Currently issued to a member.</summary>
    Borrowed,

    /// <summary>Reported lost - not issuable.</summary>
    Lost,

    /// <summary>Physically damaged - not issuable until repaired.</summary>
    Damaged,

    /// <summary>Withdrawn for maintenance/binding - not issuable.</summary>
    Maintenance
}
