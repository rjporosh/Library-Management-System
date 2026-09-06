namespace Library.Application.Features.Books.Models;

/// <summary>Editable book fields.</summary>
public sealed record UpdateBookRequest(
    string ISBN,
    string Title,
    string Author,
    int PublishedYear,
    string Category,
    string Publisher,
    string? Description = null);
