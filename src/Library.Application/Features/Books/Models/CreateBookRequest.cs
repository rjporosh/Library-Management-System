namespace Library.Application.Features.Books.Models;

/// <summary>
/// Represents the information required to create a new book.
/// </summary>
/// <param name="ISBN">The International Standard Book Number of the book.</param>
/// <param name="Title">The title of the book.</param>
/// <param name="Author">The author of the book.</param>
/// <param name="PublishedYear">The year in which the book was published.</param>
/// <param name="Description">An optional description of the book.</param>
public sealed record CreateBookRequest(
    string ISBN,
    string Title,
    string Author,
    int PublishedYear,
    string? Description);