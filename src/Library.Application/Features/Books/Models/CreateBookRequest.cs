namespace Library.Application.Features.Books.Models;

/// <summary>Information required to create a new book.</summary>
/// <param name="TotalCopies">When &gt; 0, this many <c>BookCopy</c> rows are generated with sequential barcodes.</param>
public sealed record CreateBookRequest(
    string ISBN,
    string Title,
    string Author,
    int PublishedYear,
    string Category,
    string Publisher,
    string? Description = null,
    string? CoverImageUrl = null,
    string? Edition = null,
    bool HasEbook = false,
    string? EbookUrl = null,
    bool HasAudiobook = false,
    string? AudiobookUrl = null,
    string? ExternalBuyUrl = null,
    string? ExternalPdfUrl = null,
    int TotalCopies = 0);
