using Library.Domain.Enums;

namespace Library.Application.Features.BookCopies.Models;

public sealed record BookCopyResponse(
    Guid Id,
    Guid BookId,
    string Barcode,
    BookCopyStatus Status);