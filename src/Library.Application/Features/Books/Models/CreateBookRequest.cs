namespace Library.Application.Features.Books.Models;

/// <summary>Information required to create a new book.</summary>
public sealed record CreateBookRequest(
    string ISBN,
    string Title,
    string Author,
    int PublishedYear,
    string Category,
    string Publisher,
    string? Description = null);
