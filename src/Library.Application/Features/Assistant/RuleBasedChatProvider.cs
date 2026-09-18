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

        var copiesMatch = CopiesOfPattern().Match(lower);
        if (copiesMatch.Success)
        {
            var title = ExtractQuoted(text) ?? CleanTitle(copiesMatch.Groups["title"].Value);
            return new ChatAnswer(await AnswerCopyCountAsync(title, cancellationToken), Name);
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
            "- \"What are the most borrowed books this month?\"\n" +
            "- \"Who borrowed the most books last month?\"\n" +
            "Try rephrasing your question along those lines.",
            Name);
    }

    private async Task<string> AnswerCopyCountAsync(string title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Which book would you like the copy count for?";
        }

        var results = await tools.GetCopyCountAsync(title, cancellationToken);
        if (results.Count == 0)
        {
            return $"I couldn't find any book matching \"{title}\".";
        }

        return string.Join("\n", results.Select(r =>
            $"\"{r.Title}\" by {r.Author}: {r.AvailableCopies} of {r.TotalCopies} copies available."));
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

    private static string? ExtractQuoted(string text)
    {
        var match = QuotedPattern().Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Strips trailing filler ("are available", "in stock", "left", the
    /// question mark, etc.) that the copies-of regex otherwise swallows into
    /// the title, e.g. "clean code are available?" -> "clean code".
    /// </summary>
    private static string CleanTitle(string raw)
    {
        var title = TrailingFillerPattern().Replace(raw, "").Trim(' ', '?', '.', '"', '\'');
        return title;
    }

    [GeneratedRegex(@"\s*(?:are|is)?\s*(?:available|in\s+stock|left|remaining|in\s+the\s+library)\s*[?.]*\s*$")]
    private static partial Regex TrailingFillerPattern();

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

    [GeneratedRegex("\"([^\"]+)\"")]
    private static partial Regex QuotedPattern();

    [GeneratedRegex(@"last\s+(\d+)\s+days?")]
    private static partial Regex DaysPattern();
}
