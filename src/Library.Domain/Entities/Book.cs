namespace Library.Domain.Entities;

public sealed class Book
{
    public Guid Id { get; init; }

    public string ISBN { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int PublishedYear { get; private set; }

    public Book(
        Guid id,
        string isbn,
        string title,
        string author,
        int publishedYear,
        string? description = null)
    {
        Id = id;
        Update(
            isbn,
            title,
            author,
            publishedYear,
            description);
    }

    public void Update(
        string isbn,
        string title,
        string author,
        int publishedYear,
        string? description = null)
    {
        ISBN = isbn;
        Title = title;
        Author = author;
        PublishedYear = publishedYear;
        Description = description;
    }
}
