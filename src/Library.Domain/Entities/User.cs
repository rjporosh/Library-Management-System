using Library.Domain.Common;
using Library.Domain.Enums;

namespace Library.Domain.Entities;

/// <summary>
/// A login account. Librarians are staff accounts with full catalog/member
/// access. Member accounts are linked 1:1 to a <see cref="Member"/> row via
/// <see cref="MemberId"/> so a member can only ever see/act on their own
/// borrowing data.
/// </summary>
public sealed class User : Entity
{
    public string Username { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    /// <summary>Set only for <see cref="UserRole.Member"/> accounts.</summary>
    public Guid? MemberId { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    // EF Core materialisation only.
    private User() { }

    public User(
        Guid id,
        string username,
        string email,
        string passwordHash,
        UserRole role,
        Guid? memberId = null)
        : base(id)
    {
        if (role == UserRole.Member && memberId is null)
        {
            throw new ArgumentException("A member account must be linked to a Member.", nameof(memberId));
        }

        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        MemberId = memberId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void RecordLogin() => LastLoginAtUtc = DateTime.UtcNow;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
