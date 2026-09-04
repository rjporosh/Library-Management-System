using Library.Application.Common.Search;
using Library.Domain.Entities;

namespace Library.Application.Features.Books;

/// <summary>Whitelisted advanced-search fields for the book catalog.</summary>
public static class BookSearchMap
{
    public static readonly SearchFieldMap<Book> Fields = new SearchFieldMap<Book>()
        .Field("title", b => b.Title, quickSearch: true)
        .Field("author", b => b.Author, quickSearch: true)
        .Field("isbn", b => b.ISBN, quickSearch: true)
        .Field("description", b => b.Description)
        .Field("publishedYear", b => b.PublishedYear);
}
