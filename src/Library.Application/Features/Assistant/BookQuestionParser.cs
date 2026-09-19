using System.Text.RegularExpressions;

namespace Library.Application.Features.Assistant;

public enum CopyMetric
{
    Total,
    Available,
    Borrowed,
}

/// <summary>A copy-count question decomposed into the metric asked for and the book criteria.</summary>
public sealed record BookQuestion(CopyMetric Metric, BookFilter Filter);

/// <summary>
/// Understands English copy-count questions such as "how many copies of Clean
/// Code are borrowed", "how many books by Robert Martin do we have", "how many
/// borrowed copies of the second edition of Refactoring published by
/// Addison-Wesley". Pure and deterministic (voice transcripts and typed text go
/// through the same path); anything it cannot recognise returns null so the
/// caller can fall through to the other intents.
/// </summary>
public static partial class BookQuestionParser
{
    /// <summary>Words that end a captured value ("... Robert Martin are borrowed" -> "Robert Martin").</summary>
    private const string Stop =
        @"(?:publisher|published|edition|author|written|isbn|are|is|have|has|had|were|was|been|currently|now|that|which|and|do|does|in|still|available|borrowed|issued|lent|left|remaining|total)";

    private static readonly string[] GenericNouns = ["book", "books", "copy", "copies", "all", "any", "library", "the library", "titles", "title"];

    public static BookQuestion? Parse(string message)
    {
        var text = WhitespacePattern().Replace(message.Trim().Trim('?', '.', '!', ' '), " ");
        var lower = text.ToLowerInvariant();

        if (!CountIntentPattern().IsMatch(lower) || ExcludedPattern().IsMatch(lower))
        {
            return null;
        }

        var metric = MetricOf(lower);
        var quoted = QuotedPattern().Match(text);

        var edition = Take(EditionAfterPattern(), ref text) ?? Take(EditionBeforePattern(), ref text);
        var publisher = Take(PublisherPattern(), ref text);
        var author = Take(AuthorPattern(), ref text);

        string? title = quoted.Success ? quoted.Groups[1].Value : null;
        if (title is null)
        {
            var match = TitlePattern().Match(text);
            title = match.Success ? CleanTitle(match.Groups["v"].Value) : null;
        }

        if (title is not null && GenericNouns.Contains(title.ToLowerInvariant()))
        {
            title = null;
        }

        var filter = new BookFilter(title, author, publisher, edition);
        var hasCriteria = title is not null || author is not null || publisher is not null || edition is not null;
        return hasCriteria ? new BookQuestion(metric, filter) : null;
    }

    private static CopyMetric MetricOf(string lower)
    {
        if (BorrowedPattern().IsMatch(lower))
        {
            return CopyMetric.Borrowed;
        }

        return AvailablePattern().IsMatch(lower) ? CopyMetric.Available : CopyMetric.Total;
    }

    /// <summary>Captures group "v" of <paramref name="pattern"/> and removes the whole match from the text.</summary>
    private static string? Take(Regex pattern, ref string text)
    {
        var match = pattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["v"].Value.Trim(' ', ',', '"', '\'');
        text = WhitespacePattern().Replace(text.Remove(match.Index, match.Length), " ").Trim();
        return value.Length == 0 ? null : value;
    }

    private static string? CleanTitle(string raw)
    {
        var title = TrailingFillerPattern().Replace(raw, "").Trim(' ', '?', '.', ',', '"', '\'');
        return title.Length == 0 ? null : title;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex("\"([^\"]+)\"|“([^”]+)”")]
    private static partial Regex QuotedPattern();

    [GeneratedRegex(@"\b(?:how\s+many|number\s+of|count\s+of|total|how\s+much)\b")]
    private static partial Regex CountIntentPattern();

    // "most borrowed books" / "who borrowed the most" belong to the ranking intents.
    [GeneratedRegex(@"\b(?:most|top|who|which\s+member|members?\s+(?:have|has))\b")]
    private static partial Regex ExcludedPattern();

    [GeneratedRegex(@"\b(?:borrow\w*|issued|checked\s+out|lent|on\s+loan|taken)\b")]
    private static partial Regex BorrowedPattern();

    [GeneratedRegex(@"\b(?:available|in\s+stock|left|remaining|on\s+the\s+shelf)\b")]
    private static partial Regex AvailablePattern();

    [GeneratedRegex(@"\b(?:the\s+)?edition\s+(?:number\s+)?(?<v>\d+\w*|first|second|third|fourth|fifth|sixth|seventh|eighth|ninth|tenth)\b", RegexOptions.IgnoreCase)]
    private static partial Regex EditionAfterPattern();

    [GeneratedRegex(@"\b(?:the\s+)?(?<v>\d+(?:st|nd|rd|th)?|first|second|third|fourth|fifth|sixth|seventh|eighth|ninth|tenth)\s+edition\b(?:\s+of)?", RegexOptions.IgnoreCase)]
    private static partial Regex EditionBeforePattern();

    [GeneratedRegex(@"\b(?:published\s+by|publisher(?:\s+named|\s+called)?|from\s+publisher)\s+(?<v>.+?)(?=\s+" + Stop + @"\b|$)", RegexOptions.IgnoreCase)]
    private static partial Regex PublisherPattern();

    [GeneratedRegex(@"\b(?:written\s+by|authored\s+by|by\s+author|author(?:\s+named|\s+called)?|by)\s+(?<v>.+?)(?=\s+" + Stop + @"\b|$)", RegexOptions.IgnoreCase)]
    private static partial Regex AuthorPattern();

    [GeneratedRegex(@"\b(?:of|for|named|called|titled)\s+(?:the\s+)?(?:books?\s+)?(?:(?:named|called|titled)\s+)?(?<v>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TitlePattern();

    [GeneratedRegex(@"\s*\b(?:(?:are|is|have|has|were|was)\s+)?(?:(?:been|currently|now|still|all)\s+)*(?:available|in\s+stock|left|remaining|borrowed|issued|lent|checked\s+out|on\s+loan|in\s+the\s+library|in\s+total|total|copies|do\s+(?:we|you)\s+have|does\s+the\s+library\s+have)\b.*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingFillerPattern();
}
