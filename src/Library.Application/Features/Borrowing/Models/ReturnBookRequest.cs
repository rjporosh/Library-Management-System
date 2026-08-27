namespace Library.Application.Features.Borrowing.Models;

public sealed record ReturnBookRequest(
    DateTime? ReturnedAt = null);
