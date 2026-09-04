using Library.Domain.Enums;

namespace Library.Application.Features.Members.Models;

/// <summary>A library member as returned by the API.</summary>
public sealed record MemberResponse(
    Guid Id,
    string MembershipNumber,
    string Name,
    string Email,
    MemberStatus Status,
    DateTime MembershipExpiresAt,
    DateTime? SuspendedAt = null,
    DateTime? LastRenewedAt = null);

/// <summary>A member plus their borrowing summary (member detail view).</summary>
public sealed record MemberDetailResponse(
    MemberResponse Member,
    int TotalBorrowed,
    int CurrentlyBorrowed,
    int Overdue,
    DateTime? LastBorrowedAt,
    IReadOnlyList<MemberBorrowSummary> History);

public sealed record MemberBorrowSummary(
    Guid BorrowRecordId,
    Guid BookCopyId,
    DateTime BorrowedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    BorrowStatus Status,
    bool IsOverdue);
