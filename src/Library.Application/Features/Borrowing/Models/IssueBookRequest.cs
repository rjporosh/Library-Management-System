namespace Library.Application.Features.Borrowing.Models;

public sealed record IssueBookRequest(
    Guid MemberId,
    Guid BookCopyId,
    DateTime DueAt);
