using Library.Domain.Enums;

namespace Library.Application.Features.BookCopies.Models;

/// <param name="BookTitle">Denormalized for display/search so the UI never has to show a bare book id.</param>
public sealed record BookCopyResponse(
    Guid Id,
    Guid BookId,
    string Barcode,
    BookCopyStatus Status,
    string BookTitle = "");
