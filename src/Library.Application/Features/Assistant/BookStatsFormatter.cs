namespace Library.Application.Features.Assistant;

/// <summary>
/// Turns copy-count results into a short, natural sentence. The text doubles as
/// the spoken answer in the chat widget, so it avoids markup and tabular layout.
/// </summary>
public static class BookStatsFormatter
{
    public static string Describe(BookQuestion question, IReadOnlyList<BookCopyCount> counts)
    {
        if (counts.Count == 0)
        {
            return $"I couldn't find any book matching {DescribeFilter(question.Filter)}.";
        }

        var total = counts.Sum(c => c.TotalCopies);
        var available = counts.Sum(c => c.AvailableCopies);
        var borrowed = counts.Sum(c => c.BorrowedCopies);

        var headline = question.Metric switch
        {
            CopyMetric.Borrowed => $"{Copies(borrowed)} of {DescribeFilter(question.Filter)} {(borrowed == 1 ? "is" : "are")} currently borrowed.",
            CopyMetric.Available => $"{Copies(available)} of {DescribeFilter(question.Filter)} {(available == 1 ? "is" : "are")} available.",
            _ => $"The library has {Copies(total)} of {DescribeFilter(question.Filter)}.",
        };

        var lines = counts.Select(c =>
            $"\"{c.Title}\" by {c.Author}{Detail(c)}: {c.TotalCopies} total, {c.AvailableCopies} available, {c.BorrowedCopies} borrowed.");

        return counts.Count == 1
            ? $"{headline}\n{lines.First()}"
            : $"{headline} Across {counts.Count} matching books: {total} total, {available} available, {borrowed} borrowed.\n{string.Join("\n", lines)}";
    }

    public static string DescribeFilter(BookFilter filter)
    {
        var parts = new List<string>();
        if (filter.Title is not null) parts.Add($"\"{filter.Title}\"");
        if (filter.Edition is not null) parts.Add($"edition {filter.Edition}");
        if (filter.Author is not null) parts.Add($"author {filter.Author}");
        if (filter.Publisher is not null) parts.Add($"publisher {filter.Publisher}");
        if (filter.AnyText is not null) parts.Add($"\"{filter.AnyText}\"");
        return string.Join(", ", parts);
    }

    private static string Copies(int n) => n == 1 ? "1 copy" : $"{n} copies";

    private static string Detail(BookCopyCount c)
    {
        var details = new[] { c.Publisher, c.Edition }.Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
        return details.Count == 0 ? "" : $" ({string.Join(", ", details)})";
    }
}
