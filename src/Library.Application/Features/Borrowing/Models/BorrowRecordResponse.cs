using Library.Domain.Enums;

namespace Library.Application.Features.Borrowing.Models;

public sealed record BorrowRecordResponse(
    Guid Id,
    Guid MemberId,
    Guid BookCopyId,
    DateTime BorrowedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    BorrowStatus Status);
