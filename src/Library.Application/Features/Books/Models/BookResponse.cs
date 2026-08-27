namespace Library.Application.Features.Books.Models;

public sealed record BookResponse(
    Guid Id,
    string ISBN,
    string Title,
    string Author,
    string? Description,
    int PublishedYear);
