namespace Library.Application.Features.Members.Models;

public sealed record CreateMemberRequest(
    string MembershipNumber,
    string Name,
    string Email);