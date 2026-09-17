using Library.Domain.Enums;

namespace Library.Application.Features.Auth.Models;

public sealed record LoginRequest(string UsernameOrEmail, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Username,
    UserRole Role,
    Guid? MemberId);

/// <summary>Member self-registration: creates both the <c>Member</c> row and its login.</summary>
public sealed record RegisterMemberRequest(
    string Name,
    string Email,
    string Password,
    string Phone = "",
    string Address = "");

/// <summary>Librarian-only: provisions another staff account.</summary>
public sealed record RegisterLibrarianRequest(string Username, string Email, string Password);
