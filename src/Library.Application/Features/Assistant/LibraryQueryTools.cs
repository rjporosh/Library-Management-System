using Library.Application.Abstractions.Persistence;
using Library.Domain.Enums;

namespace Library.Application.Features.Assistant;

/// <summary>
/// The small set of read-only queries the chat assistant is allowed to run.
/// Both the rule-based engine and the LLM providers (as tool-use/function-
/// calling targets) go through exactly these methods - an LLM never gets
/// free-form database access, only these named, parameterized operations.
/// </summary>
public sealed class LibraryQueryTools(
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository,
    IBorrowRecordRepository borrowRecordRepository,
    IMemberRepository memberRepository)
{
    /// <summary>Total, available and borrowed copy counts for books matching a title/author/ISBN fragment.</summary>
    public Task<IReadOnlyList<BookCopyCount>> GetCopyCountAsync(string titleQuery, CancellationToken cancellationToken = default) =>
        GetCopyCountAsync(new BookFilter(AnyText: titleQuery), cancellationToken);

    /// <summary>
    /// Total, available and borrowed copy counts for books matching every supplied
    /// criterion (title, author, publisher, edition, or a free-text fragment of
    /// title/author/ISBN). Criteria are AND-ed; text matching is case-insensitive.
    /// </summary>
    public Task<IReadOnlyList<BookCopyCount>> GetCopyCountAsync(BookFilter filter, CancellationToken cancellationToken = default)
    {
        var query = bookRepository.Query();

        if (Normalise(filter.Title) is { } title)
        {
            query = query.Where(b => b.Title.ToLower().Contains(title));
        }

        if (Normalise(filter.Author) is { } author)
        {
            query = query.Where(b => b.Author.ToLower().Contains(author));
        }

        if (Normalise(filter.Publisher) is { } publisher)
        {
            query = query.Where(b => b.Publisher.ToLower().Contains(publisher));
        }

        if (Normalise(filter.AnyText) is { } any)
        {
            query = query.Where(b => b.Title.ToLower().Contains(any) || b.Author.ToLower().Contains(any) || b.ISBN.Contains(any));
        }

        var edition = EditionNumber(filter.Edition);
        if (filter.Edition is not null)
        {
            query = query.Where(b => b.Edition != null);
        }

        var books = query.ToList()
            .Where(b => filter.Edition is null || EditionMatches(b.Edition, filter.Edition, edition))
            .Take(MaxBooks)
            .ToList();

        if (books.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<BookCopyCount>>([]);
        }

        var bookIds = books.Select(b => b.Id).ToHashSet();
        var copies = bookCopyRepository.Query().Where(c => bookIds.Contains(c.BookId)).ToList();

        IReadOnlyList<BookCopyCount> result = [.. books.Select(b =>
        {
            var bookCopies = copies.Where(c => c.BookId == b.Id).ToList();
            return new BookCopyCount(
                b.Title,
                b.Author,
                bookCopies.Count,
                bookCopies.Count(c => c.Status == BookCopyStatus.Available),
                bookCopies.Count(c => c.Status == BookCopyStatus.Borrowed),
                b.Publisher,
                b.Edition);
        })];

        return Task.FromResult(result);
    }

    private const int MaxBooks = 10;

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    /// <summary>"2nd", "second", "2" -> 2; null when the text carries no edition number.</summary>
    private static int? EditionNumber(string? edition)
    {
        if (string.IsNullOrWhiteSpace(edition))
        {
            return null;
        }

        var text = edition.Trim().ToLowerInvariant();
        var ordinals = new[] { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth" };
        var index = Array.FindIndex(ordinals, o => text.Contains(o, StringComparison.Ordinal));
        if (index >= 0)
        {
            return index + 1;
        }

        var digits = new string([.. text.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit)]);
        return int.TryParse(digits, out var n) ? n : null;
    }

    private static bool EditionMatches(string? bookEdition, string wanted, int? wantedNumber)
    {
        if (bookEdition is null)
        {
            return false;
        }

        // "2nd Edition" must match "second edition" and "2", but "12th" must not match "2".
        return wantedNumber is { } n
            ? EditionNumber(bookEdition) == n
            : bookEdition.Contains(wanted.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The most-borrowed books in the last <paramref name="days"/> days, most first.</summary>
    public Task<IReadOnlyList<BookBorrowCount>> GetMostBorrowedBooksAsync(int days, int top, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Max(days, 1));
        var records = borrowRecordRepository.Query().Where(r => r.BorrowedAt >= since).ToList();
        if (records.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<BookBorrowCount>>([]);
        }

        var copyIds = records.Select(r => r.BookCopyId).ToHashSet();
        var copies = bookCopyRepository.Query().Where(c => copyIds.Contains(c.Id)).ToList().ToDictionary(c => c.Id);

        var bookIds = copies.Values.Select(c => c.BookId).ToHashSet();
        var books = bookRepository.Query().Where(b => bookIds.Contains(b.Id)).ToList().ToDictionary(b => b.Id);

        IReadOnlyList<BookBorrowCount> result = [.. records
            .Where(r => copies.ContainsKey(r.BookCopyId) && books.ContainsKey(copies[r.BookCopyId].BookId))
            .GroupBy(r => copies[r.BookCopyId].BookId)
            .Select(g => new BookBorrowCount(books[g.Key].Title, books[g.Key].Author, g.Count()))
            .OrderByDescending(x => x.BorrowCount)
            .Take(Math.Max(top, 1))];

        return Task.FromResult(result);
    }

    /// <summary>The members with the most borrows in the last <paramref name="days"/> days, most first.</summary>
    public Task<IReadOnlyList<MemberBorrowCount>> GetTopBorrowersAsync(int days, int top, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Max(days, 1));
        var records = borrowRecordRepository.Query().Where(r => r.BorrowedAt >= since).ToList();
        if (records.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<MemberBorrowCount>>([]);
        }

        var memberIds = records.Select(r => r.MemberId).ToHashSet();
        var members = memberRepository.Query().Where(m => memberIds.Contains(m.Id)).ToList().ToDictionary(m => m.Id);

        IReadOnlyList<MemberBorrowCount> result = [.. records
            .Where(r => members.ContainsKey(r.MemberId))
            .GroupBy(r => r.MemberId)
            .Select(g => new MemberBorrowCount(members[g.Key].Name, members[g.Key].MembershipNumber, g.Count()))
            .OrderByDescending(x => x.BorrowCount)
            .Take(Math.Max(top, 1))];

        return Task.FromResult(result);
    }
}

/// <summary>Search criteria for <see cref="LibraryQueryTools.GetCopyCountAsync(BookFilter, CancellationToken)"/>; every supplied field must match.</summary>
public sealed record BookFilter(
    string? Title = null,
    string? Author = null,
    string? Publisher = null,
    string? Edition = null,
    string? AnyText = null);

public sealed record BookCopyCount(
    string Title,
    string Author,
    int TotalCopies,
    int AvailableCopies,
    int BorrowedCopies,
    string Publisher = "",
    string? Edition = null);

public sealed record BookBorrowCount(string Title, string Author, int BorrowCount);

public sealed record MemberBorrowCount(string Name, string MembershipNumber, int BorrowCount);
