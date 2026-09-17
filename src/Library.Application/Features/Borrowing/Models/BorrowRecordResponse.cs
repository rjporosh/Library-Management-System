using Library.Domain.Enums;

namespace Library.Application.Features.Borrowing.Models;

/// <param name="MemberName">Denormalized for display/search on the borrowing page. Empty unless the caller (Search) populates it - Issue/Return responses do not.</param>
/// <param name="MembershipNumber">See <paramref name="MemberName"/>.</param>
/// <param name="BookTitle">See <paramref name="MemberName"/>.</param>
/// <param name="Barcode">See <paramref name="MemberName"/>.</param>
public sealed record BorrowRecordResponse(
    Guid Id,
    Guid MemberId,
    Guid BookCopyId,
    DateTime BorrowedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    BorrowStatus Status,
    string MemberName = "",
    string MembershipNumber = "",
    string BookTitle = "",
    string Barcode = "");
