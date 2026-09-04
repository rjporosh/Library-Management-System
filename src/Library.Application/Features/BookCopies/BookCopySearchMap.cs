using Library.Application.Common.Search;
using Library.Domain.Entities;

namespace Library.Application.Features.BookCopies;

/// <summary>Whitelisted advanced-search fields for book copies. Status is matched by name.</summary>
public static class BookCopySearchMap
{
    public static readonly SearchFieldMap<BookCopy> Fields = new SearchFieldMap<BookCopy>()
        .Field("barcode", c => c.Barcode, quickSearch: true)
        .Field("status", c => c.Status)
        .Field("bookId", c => c.BookId);
}
