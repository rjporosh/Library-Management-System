using System.ComponentModel.DataAnnotations;

namespace Library.Application.Features.Books.Models;

public sealed record UpdateBookRequest(
    [property: Required]
    string ISBN,

    [property: Required]
    string Title,

    [property: Required]
    string Author,

    [property: Range(0, 9999)]
    int PublishedYear,

    string? Description);
