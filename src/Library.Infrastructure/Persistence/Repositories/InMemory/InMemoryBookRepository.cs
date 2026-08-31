using Library.Application.Abstractions.Persistence;
using Library.Application.Features.Books.Models;
using Library.Domain.Entities;

namespace Library.Infrastructure.Persistence.Repositories.InMemory;

public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly List<Book> _books = [];

    public Task<(IReadOnlyList<Book> Items, int TotalItems)> GetAsync(
        BookQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<Book> filteredBooks = _books;

        var search = query.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFields = ParseSearchFields(query.SearchBy);

            filteredBooks = filteredBooks.Where(book =>
                (searchFields.Contains(BookSearchField.Title) &&
                 book.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
                ||
                (searchFields.Contains(BookSearchField.Author) &&
                 book.Author.Contains(search, StringComparison.OrdinalIgnoreCase))
                ||
                (searchFields.Contains(BookSearchField.ISBN) &&
                 book.ISBN.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var totalItems = filteredBooks.Count();

        filteredBooks = ApplySorting(
            filteredBooks,
            query.SortBy,
            query.SortDirection);

        var items = filteredBooks
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult(
            ((IReadOnlyList<Book>)items, totalItems));
    }

    public Task<Book?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var book = _books.FirstOrDefault(x => x.Id == id);

        return Task.FromResult(book);
    }

    public Task AddAsync(
        Book book,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _books.Add(book);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Book book,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var index = _books.FindIndex(x => x.Id == book.Id);

        if (index >= 0)
        {
            _books[index] = book;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Book book,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _books.Remove(book);

        return Task.CompletedTask;
    }

    public void Seed(IEnumerable<Book> books)
    {
        _books.AddRange(books);
    }

    private static HashSet<BookSearchField> ParseSearchFields(
        string? searchBy)
    {
        if (string.IsNullOrWhiteSpace(searchBy))
        {
            return [BookSearchField.Title];
        }

        var fields = new HashSet<BookSearchField>();

        foreach (var value in searchBy.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<BookSearchField>(
                    value.Trim(),
                    ignoreCase: true,
                    out var field))
            {
                fields.Add(field);
            }
        }

        return fields.Count == 0
            ? [BookSearchField.Title]
            : fields;
    }

    private static IEnumerable<Book> ApplySorting(
        IEnumerable<Book> books,
        string? sortBy,
        string? sortDirection)
    {
        var sortFields = ParseSortFields(sortBy);

        if (sortFields.Count == 0)
        {
            return books.OrderBy(book => book.Title);
        }

        var descending = string.Equals(
            sortDirection?.Trim(),
            "desc",
            StringComparison.OrdinalIgnoreCase);

        IOrderedEnumerable<Book>? orderedBooks = null;

        foreach (var field in sortFields)
        {
            orderedBooks = orderedBooks is null
                ? ApplyFirstSort(books, field, descending)
                : ApplyThenSort(orderedBooks, field, descending);
        }

        return orderedBooks!;
    }

    private static IOrderedEnumerable<Book> ApplyFirstSort(
        IEnumerable<Book> books,
        BookSortField field,
        bool descending)
    {
        return field switch
        {
            BookSortField.Title => descending
                ? books.OrderByDescending(book => book.Title)
                : books.OrderBy(book => book.Title),

            BookSortField.Author => descending
                ? books.OrderByDescending(book => book.Author)
                : books.OrderBy(book => book.Author),

            BookSortField.ISBN => descending
                ? books.OrderByDescending(book => book.ISBN)
                : books.OrderBy(book => book.ISBN),

            BookSortField.PublishedYear => descending
                ? books.OrderByDescending(book => book.PublishedYear)
                : books.OrderBy(book => book.PublishedYear),

            _ => books.OrderBy(book => book.Title)
        };
    }

    private static IOrderedEnumerable<Book> ApplyThenSort(
        IOrderedEnumerable<Book> books,
        BookSortField field,
        bool descending)
    {
        return field switch
        {
            BookSortField.Title => descending
                ? books.ThenByDescending(book => book.Title)
                : books.ThenBy(book => book.Title),

            BookSortField.Author => descending
                ? books.ThenByDescending(book => book.Author)
                : books.ThenBy(book => book.Author),

            BookSortField.ISBN => descending
                ? books.ThenByDescending(book => book.ISBN)
                : books.ThenBy(book => book.ISBN),

            BookSortField.PublishedYear => descending
                ? books.ThenByDescending(book => book.PublishedYear)
                : books.ThenBy(book => book.PublishedYear),

            _ => books.ThenBy(book => book.Title)
        };
    }

    private static List<BookSortField> ParseSortFields(
        string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return [];
        }

        var fields = new List<BookSortField>();

        foreach (var value in sortBy.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<BookSortField>(
                    value.Trim(),
                    ignoreCase: true,
                    out var field)
                && !fields.Contains(field))
            {
                fields.Add(field);
            }
        }

        return fields;
    }
}
