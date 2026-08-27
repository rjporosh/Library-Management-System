namespace Library.Application.Features.BookCopies.Models;

public sealed record CreateBookCopyRequest(
    Guid BookId,
    string Barcode);