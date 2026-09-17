namespace Library.Application.Features.Books.Models;

/// <summary>
/// The "smart availability" resolution for a book: physical copy first,
/// then ebook, then audiobook, then an external buy/PDF suggestion when
/// none of the above are available.
/// </summary>
public enum BookAvailabilityStatus
{
    PhysicalAvailable,
    Ebook,
    Audiobook,
    Unavailable
}

/// <param name="AccessUrl">The ebook/audiobook link when <see cref="Status"/> is <c>Ebook</c>/<c>Audiobook</c>.</param>
/// <param name="ExternalBuyUrl">Suggested purchase link, populated only when <see cref="Status"/> is <c>Unavailable</c>.</param>
/// <param name="ExternalPdfUrl">Suggested free/online PDF link, populated only when <see cref="Status"/> is <c>Unavailable</c>.</param>
public sealed record BookAvailabilitySummary(
    BookAvailabilityStatus Status,
    int TotalCopies,
    int AvailableCopies,
    string? AccessUrl,
    string? ExternalBuyUrl,
    string? ExternalPdfUrl);

public sealed record BookDetailResponse(BookResponse Book, BookAvailabilitySummary Availability);
