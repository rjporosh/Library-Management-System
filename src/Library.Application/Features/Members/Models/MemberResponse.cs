using Library.Domain.Enums;

namespace Library.Application.Features.Members.Models;

public sealed record MemberResponse(
    Guid Id,
    string MembershipNumber,
    string Name,
    string Email,
    MemberStatus Status,
    DateTime? SuspendedAt = null,
    DateTime? LastRenewedAt = null);