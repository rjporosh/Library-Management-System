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
    /// <summary>Total and available copy counts for books matching a title/author/ISBN fragment.</summary>
    public Task<IReadOnlyList<BookCopyCount>> GetCopyCountAsync(string titleQuery, CancellationToken cancellationToken = default)
    {
        var term = titleQuery.Trim().ToLowerInvariant();
        var books = bookRepository.Query()
            .Where(b => b.Title.ToLower().Contains(term) || b.Author.ToLower().Contains(term) || b.ISBN.Contains(term))
            .Take(5)
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
                bookCopies.Count(c => c.Status == BookCopyStatus.Available));
        })];

        return Task.FromResult(result);
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

public sealed record BookCopyCount(string Title, string Author, int TotalCopies, int AvailableCopies);

public sealed record BookBorrowCount(string Title, string Author, int BorrowCount);

public sealed record MemberBorrowCount(string Name, string MembershipNumber, int BorrowCount);
