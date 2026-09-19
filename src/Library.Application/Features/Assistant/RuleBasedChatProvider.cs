using System.Text.RegularExpressions;

namespace Library.Application.Features.Assistant;

/// <summary>
/// Deterministic keyword/regex intent matching over <see cref="LibraryQueryTools"/>.
/// No external dependency, no cost, always available - the default provider
/// and the fallback whenever an LLM provider is selected but unconfigured.
/// </summary>
public sealed partial class RuleBasedChatProvider(LibraryQueryTools tools) : IChatProvider
{
    public string Name => "RuleBased";

    public async Task<ChatAnswer> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        var text = message.Trim();
        var lower = text.ToLowerInvariant();

        var question = BookQuestionParser.Parse(text);
        if (question is not null)
        {
            var counts = await tools.GetCopyCountAsync(question.Filter, cancellationToken);
            return new ChatAnswer(BookStatsFormatter.Describe(question, counts), Name);
        }

        if (CopiesOfPattern().IsMatch(lower))
        {
            return new ChatAnswer("Which book, author, publisher or edition would you like the copy count for?", Name);
        }

        if (MostBorrowedPattern().IsMatch(lower))
        {
            var days = ExtractDays(lower) ?? 30;
            var results = await tools.GetMostBorrowedBooksAsync(days, 5, cancellationToken);
            return new ChatAnswer(FormatMostBorrowed(results, days), Name);
        }

        if (TopBorrowerPattern().IsMatch(lower))
        {
            var days = ExtractDays(lower) ?? 30;
            var results = await tools.GetTopBorrowersAsync(days, 5, cancellationToken);
            return new ChatAnswer(FormatTopBorrowers(results, days), Name);
        }

        return new ChatAnswer(
            "I can answer questions like:\n" +
            "- \"How many copies of Clean Code are available?\"\n" +
            "- \"How many copies of books by Robert C. Martin are borrowed?\"\n" +
            "- \"How many borrowed copies of the second edition of Refactoring?\"\n" +
            "- \"What are the most borrowed books this month?\"\n" +
            "- \"Who borrowed the most books last month?\"\n" +
            "Try rephrasing your question along those lines.",
            Name);
    }

    private static string FormatMostBorrowed(IReadOnlyList<BookBorrowCount> results, int days)
    {
        if (results.Count == 0)
        {
            return $"No borrows recorded in the last {days} days.";
        }

        var lines = results.Select((r, i) => $"{i + 1}. \"{r.Title}\" by {r.Author} - {r.BorrowCount} borrow(s)");
        return $"Most borrowed in the last {days} days:\n" + string.Join("\n", lines);
    }

    private static string FormatTopBorrowers(IReadOnlyList<MemberBorrowCount> results, int days)
    {
        if (results.Count == 0)
        {
            return $"No borrows recorded in the last {days} days.";
        }

        var lines = results.Select((r, i) => $"{i + 1}. {r.Name} ({r.MembershipNumber}) - {r.BorrowCount} borrow(s)");
        return $"Top borrowers in the last {days} days:\n" + string.Join("\n", lines);
    }

    private static int? ExtractDays(string lower)
    {
        if (lower.Contains("this week") || lower.Contains("last week")) return 7;
        if (lower.Contains("this month") || lower.Contains("last month")) return 30;
        if (lower.Contains("this year") || lower.Contains("last year")) return 365;
        var m = DaysPattern().Match(lower);
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }

    [GeneratedRegex(@"(?:how many|available)\s+cop(?:y|ies)\s+(?:of|for)?\s*(?<title>.+)")]
    private static partial Regex CopiesOfPattern();

    [GeneratedRegex(@"most\s+(?:borrowed|popular|demand(?:ed|ing)?)")]
    private static partial Regex MostBorrowedPattern();

    [GeneratedRegex(@"(?:top\s+borrower|who\s+borrowed\s+the\s+most|most\s+active\s+member)")]
    private static partial Regex TopBorrowerPattern();

    [GeneratedRegex(@"last\s+(\d+)\s+days?")]
    private static partial Regex DaysPattern();
}
