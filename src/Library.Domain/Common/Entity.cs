namespace Library.Domain.Common;

/// <summary>
/// Base type for every aggregate root: a GUID identity plus a soft-delete
/// lifecycle. Nothing is ever physically removed by the application - a
/// deleted entity is flagged and filtered out of normal queries, so history
/// and audit trails stay intact and an accidental delete is reversible.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }

    /// <summary>True once <see cref="MarkDeleted"/> has been called.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>When the entity was soft-deleted (UTC), or null.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        Id = id;
    }

    /// <summary>Soft-deletes the entity. Idempotent.</summary>
    public void MarkDeleted()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Un-deletes a previously soft-deleted entity.</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
    }
}
