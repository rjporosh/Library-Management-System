namespace Library.Application.Features.Books.Models;

/// <summary>Editable book fields.</summary>
public sealed record UpdateBookRequest(
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
    string? ExternalPdfUrl = null);
