namespace Library.Application.Features.Members.Models;

/// <summary>Information required to enroll a new member.</summary>
public sealed record CreateMemberRequest(
    string MembershipNumber,
    string Name,
    string Email);

/// <summary>Editable member fields.</summary>
public sealed record UpdateMemberRequest(
    string MembershipNumber,
    string Name,
    string Email);
