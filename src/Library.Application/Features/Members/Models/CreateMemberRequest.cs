namespace Library.Application.Features.Members.Models;

public sealed record CreateMemberRequest(
    string Name,
    string Email);