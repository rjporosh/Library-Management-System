namespace Library.Application.Features.BookCopies.Models;

/// <summary>Information required to register a new physical copy.</summary>
public sealed record CreateBookCopyRequest(
    Guid BookId,
    string Barcode);

/// <summary>Editable book-copy fields.</summary>
public sealed record UpdateBookCopyRequest(
    string Barcode);

/// <summary>Requests a condition change (Available / Lost / Damaged / Maintenance).</summary>
public sealed record ChangeBookCopyStatusRequest(
    string Status);
