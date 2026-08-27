using Library.Domain.Enums;

namespace Library.Domain.Entities;

public sealed class Member
{
    public Guid Id { get; init; }

    public string MembershipNumber { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public MemberStatus Status { get; private set; }

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

    public void Suspend()
    {
        Status = MemberStatus.Suspended;
    }
}