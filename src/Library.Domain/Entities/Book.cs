using Library.Domain.Common;

namespace Library.Domain.Entities;

public sealed class Book : Entity
{
    public string ISBN { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string Publisher { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int PublishedYear { get; private set; }

    // EF Core materialisation only.
    private Book()
    {
    }

    public Book(
        Guid id,
        string isbn,
        string title,
        string author,
        int publishedYear,
        string? description = null,
        string category = "General",
        string publisher = "Unknown")
        : base(id)
    {
        Update(isbn, title, author, publishedYear, description, category, publisher);
    }

    public void Update(
        string isbn,
        string title,
        string author,
        int publishedYear,
        string? description = null,
        string category = "General",
        string publisher = "Unknown")
    {
        ISBN = isbn;
        Title = title;
        Author = author;
        PublishedYear = publishedYear;
        Description = description;
        Category = category;
        Publisher = publisher;
    }
}
