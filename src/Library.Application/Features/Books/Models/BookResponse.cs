namespace Library.Application.Features.Books.Models;

/// <summary>A book as returned by the library API.</summary>
public sealed record BookResponse(
    Guid Id,
    string ISBN,
    string Title,
    string Author,
    string Category,
    string Publisher,
    string? Description,
    int PublishedYear);
