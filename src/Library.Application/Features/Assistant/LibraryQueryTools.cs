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

    public static int GlobalThreshold { get; set; } = 1;

    private static bool MatchesAuthor(string bookAuthor, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        var b = bookAuthor.ToLowerInvariant();
        var q = query.Trim().ToLowerInvariant();
        if (b.Contains(q)) return true;

        if (q.Contains("মার্টিন") && b.Contains("martin")) return true;
        if (q.Contains("রবার্ট") && b.Contains("robert")) return true;
        if (q.Contains("ফাউলার") && b.Contains("fowler")) return true;
        if (q.Contains("ইভান্স") && b.Contains("evans")) return true;
        if (q.Contains("থমাস") && b.Contains("thomas")) return true;
        if (q.Contains("হান্ট") && b.Contains("hunt")) return true;
        if (q.Contains("হুমায়ূন") && b.Contains("humayun")) return true;

        return false;
    }

    private static bool MatchesPublisher(string bookPublisher, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        var p = bookPublisher.ToLowerInvariant();
        var q = query.Trim().ToLowerInvariant();
        if (p.Contains(q)) return true;

        if (q.Contains("অ্যাডিসন") && p.Contains("addison")) return true;
        if (q.Contains("প্রেন্টিস") && p.Contains("prentice")) return true;

        return false;
    }

    public Task<AuthorBooksSummary> GetAuthorBooksSummaryAsync(string authorName, CancellationToken cancellationToken = default)
    {
        var books = bookRepository.Query()
            .Where(b => MatchesAuthor(b.Author, authorName))
            .ToList();

        if (books.Count == 0)
        {
            return Task.FromResult(new AuthorBooksSummary(authorName, 0, 0, 0, 0, []));
        }

        var bookIds = books.Select(b => b.Id).ToHashSet();
        var copies = bookCopyRepository.Query().Where(c => bookIds.Contains(c.BookId)).ToList();

        var bookCounts = books.Select(b =>
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
        }).ToList();

        var total = bookCounts.Sum(b => b.TotalCopies);
        var available = bookCounts.Sum(b => b.AvailableCopies);
        var borrowed = bookCounts.Sum(b => b.BorrowedCopies);

        return Task.FromResult(new AuthorBooksSummary(authorName, books.Count, total, available, borrowed, bookCounts));
    }

    public Task<PublisherBooksSummary> GetPublisherBooksSummaryAsync(string publisherName, CancellationToken cancellationToken = default)
    {
        var norm = Normalise(publisherName);
        var books = bookRepository.Query()
            .Where(b => norm == null || b.Publisher.ToLower().Contains(norm))
            .ToList();

        if (books.Count == 0)
        {
            return Task.FromResult(new PublisherBooksSummary(publisherName, 0, 0, 0, 0, []));
        }

        var bookIds = books.Select(b => b.Id).ToHashSet();
        var copies = bookCopyRepository.Query().Where(c => bookIds.Contains(c.BookId)).ToList();

        var bookCounts = books.Select(b =>
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
        }).ToList();

        var total = bookCounts.Sum(b => b.TotalCopies);
        var available = bookCounts.Sum(b => b.AvailableCopies);
        var borrowed = bookCounts.Sum(b => b.BorrowedCopies);

        return Task.FromResult(new PublisherBooksSummary(publisherName, books.Count, total, available, borrowed, bookCounts));
    }

    public Task<BorrowTimeframeSummary> GetBorrowsByTargetAndTimeframeAsync(
        string? target, BorrowTargetKind kind, Timeframe timeframe, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var since = timeframe switch
        {
            Timeframe.Today => now.Date,
            Timeframe.ThisWeek => now.AddDays(-7),
            Timeframe.ThisMonth => now.AddDays(-30),
            _ => DateTime.MinValue
        };

        var allRecords = borrowRecordRepository.Query().ToList();
        var periodRecords = allRecords.Where(r => r.BorrowedAt >= since).ToList();

        var copyIds = allRecords.Select(r => r.BookCopyId).ToHashSet();
        var copies = bookCopyRepository.Query().Where(c => copyIds.Contains(c.Id)).ToList().ToDictionary(c => c.Id);

        var bookIds = copies.Values.Select(c => c.BookId).ToHashSet();
        var books = bookRepository.Query().Where(b => bookIds.Contains(b.Id)).ToList().ToDictionary(b => b.Id);

        var normTarget = Normalise(target);

        bool MatchesTarget(Guid bookCopyId)
        {
            if (kind == BorrowTargetKind.All || string.IsNullOrWhiteSpace(normTarget))
                return true;

            if (!copies.TryGetValue(bookCopyId, out var copy) || !books.TryGetValue(copy.BookId, out var book))
                return false;

            return kind switch
            {
                BorrowTargetKind.Author => MatchesAuthor(book.Author, target),
                BorrowTargetKind.Publisher => MatchesPublisher(book.Publisher, target),
                BorrowTargetKind.BookTitle => book.Title.ToLower().Contains(normTarget),
                _ => true
            };
        }

        var matchingPeriodRecords = periodRecords.Where(r => MatchesTarget(r.BookCopyId)).ToList();
        var totalBorrowsInPeriod = matchingPeriodRecords.Count;

        var activeRecords = allRecords.Where(r => r.Status == BorrowStatus.Active && MatchesTarget(r.BookCopyId)).ToList();
        var currentlyActiveBorrows = activeRecords.Count;

        var topBorrowedInPeriod = matchingPeriodRecords
            .Where(r => copies.ContainsKey(r.BookCopyId) && books.ContainsKey(copies[r.BookCopyId].BookId))
            .GroupBy(r => copies[r.BookCopyId].BookId)
            .Select(g => new BookBorrowCount(books[g.Key].Title, books[g.Key].Author, g.Count()))
            .OrderByDescending(x => x.BorrowCount)
            .Take(5)
            .ToList();

        return Task.FromResult(new BorrowTimeframeSummary(
            target, kind, timeframe, totalBorrowsInPeriod, currentlyActiveBorrows, topBorrowedInPeriod));
    }

    public async Task<BookBorrowCount?> GetBestBorrowedBookAsync(
        Timeframe timeframe, CancellationToken cancellationToken = default)
    {
        var days = timeframe switch
        {
            Timeframe.Today => 1,
            Timeframe.ThisWeek => 7,
            Timeframe.ThisMonth => 30,
            _ => 3650
        };

        var list = await GetMostBorrowedBooksAsync(days, 1, cancellationToken);
        return list.Count > 0 ? list[0] : null;
    }

    public Task<IReadOnlyList<LowStockBookAlert>> GetLowStockThresholdBooksAsync(
        int? threshold = null, CancellationToken cancellationToken = default)
    {
        var limit = threshold ?? GlobalThreshold;
        var allBooks = bookRepository.Query().ToList();
        var bookIds = allBooks.Select(b => b.Id).ToHashSet();
        var copies = bookCopyRepository.Query().Where(c => bookIds.Contains(c.BookId)).ToList();

        var alerts = new List<LowStockBookAlert>();
        foreach (var book in allBooks)
        {
            var bookCopies = copies.Where(c => c.BookId == book.Id).ToList();
            var total = bookCopies.Count;
            if (total == 0) continue; // Book without physical copies yet or ebook only
            var available = bookCopies.Count(c => c.Status == BookCopyStatus.Available);
            var borrowed = bookCopies.Count(c => c.Status == BookCopyStatus.Borrowed);

            if (available <= limit)
            {
                alerts.Add(new LowStockBookAlert(book.Id, book.Title, book.Author, total, borrowed, available, limit));
            }
        }

        IReadOnlyList<LowStockBookAlert> result = alerts
            .OrderBy(a => a.AvailableCopies)
            .ThenByDescending(a => a.BorrowedCopies)
            .ToList();

        return Task.FromResult(result);
    }
}

public enum Timeframe
{
    Today,
    ThisWeek,
    ThisMonth,
    TillNow
}

public enum BorrowTargetKind
{
    All,
    Author,
    Publisher,
    BookTitle
}

public sealed record AuthorBooksSummary(
    string Author,
    int BookCount,
    int TotalCopies,
    int AvailableCopies,
    int BorrowedCopies,
    IReadOnlyList<BookCopyCount> Books);

public sealed record PublisherBooksSummary(
    string Publisher,
    int BookCount,
    int TotalCopies,
    int AvailableCopies,
    int BorrowedCopies,
    IReadOnlyList<BookCopyCount> Books);

public sealed record BorrowTimeframeSummary(
    string? Target,
    BorrowTargetKind Kind,
    Timeframe Timeframe,
    int TotalBorrowsInPeriod,
    int CurrentlyActiveBorrows,
    IReadOnlyList<BookBorrowCount> TopBorrowedInPeriod);

public sealed record LowStockBookAlert(
    Guid BookId,
    string Title,
    string Author,
    int TotalCopies,
    int BorrowedCopies,
    int AvailableCopies,
    int Threshold);

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
