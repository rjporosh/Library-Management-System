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

    /// <summary>Cover thumbnail URL. Optional - the frontend falls back to a
    /// derived Open Library cover URL (by ISBN) when this is null, and to a
    /// generic placeholder if that also fails to load.</summary>
    public string? CoverImageUrl { get; private set; }

    /// <summary>Optional edition label (e.g. "2nd Edition"). Shown only when present.</summary>
    public string? Edition { get; private set; }

    public bool HasEbook { get; private set; }

    public string? EbookUrl { get; private set; }

    public bool HasAudiobook { get; private set; }

    public string? AudiobookUrl { get; private set; }

    /// <summary>Suggested purchase link, shown when no physical/ebook/audiobook copy is available.</summary>
    public string? ExternalBuyUrl { get; private set; }

    /// <summary>Suggested free/online PDF link, shown alongside <see cref="ExternalBuyUrl"/> when nothing is available.</summary>
    public string? ExternalPdfUrl { get; private set; }

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
        string publisher = "Unknown",
        string? coverImageUrl = null,
        string? edition = null,
        bool hasEbook = false,
        string? ebookUrl = null,
        bool hasAudiobook = false,
        string? audiobookUrl = null,
        string? externalBuyUrl = null,
        string? externalPdfUrl = null)
        : base(id)
    {
        Update(
            isbn, title, author, publishedYear, description, category, publisher,
            coverImageUrl, edition, hasEbook, ebookUrl, hasAudiobook, audiobookUrl,
            externalBuyUrl, externalPdfUrl);
    }

    public void Update(
        string isbn,
        string title,
        string author,
        int publishedYear,
        string? description = null,
        string category = "General",
        string publisher = "Unknown",
        string? coverImageUrl = null,
        string? edition = null,
        bool hasEbook = false,
        string? ebookUrl = null,
        bool hasAudiobook = false,
        string? audiobookUrl = null,
        string? externalBuyUrl = null,
        string? externalPdfUrl = null)
    {
        ISBN = isbn;
        Title = title;
        Author = author;
        PublishedYear = publishedYear;
        Description = description;
        Category = category;
        Publisher = publisher;
        CoverImageUrl = coverImageUrl;
        Edition = edition;
        HasEbook = hasEbook;
        EbookUrl = hasEbook ? ebookUrl : null;
        HasAudiobook = hasAudiobook;
        AudiobookUrl = hasAudiobook ? audiobookUrl : null;
        ExternalBuyUrl = externalBuyUrl;
        ExternalPdfUrl = externalPdfUrl;
    }
}
