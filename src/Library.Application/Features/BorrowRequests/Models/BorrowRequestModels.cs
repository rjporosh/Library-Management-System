using Library.Domain.Enums;

namespace Library.Application.Features.BorrowRequests.Models;

/// <summary>Member self-service: request to borrow an existing title, or suggest a purchase.</summary>
public sealed record CreateBorrowRequestRequest(
    BorrowRequestType Type,
    Guid? BookId = null,
    string? SuggestedTitle = null,
    string? SuggestedAuthor = null,
    string? Note = null);

/// <param name="MemberName">Denormalized for the librarian's approval queue.</param>
/// <param name="MembershipNumber">See <paramref name="MemberName"/>.</param>
/// <param name="BookTitle">Set when <see cref="Type"/> is Borrow and the book still exists.</param>
public sealed record BorrowRequestResponse(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string MembershipNumber,
    BorrowRequestType Type,
    Guid? BookId,
    string? BookTitle,
    string? SuggestedTitle,
    string? SuggestedAuthor,
    string? Note,
    BorrowRequestStatus Status,
    DateTime RequestedAt,
    DateTime? DecidedAt,
    Guid? BorrowRecordId);
