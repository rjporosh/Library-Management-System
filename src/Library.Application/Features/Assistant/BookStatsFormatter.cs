namespace Library.Application.Features.Assistant;

/// <summary>
/// Turns copy-count results into a short, natural sentence. The text doubles as
/// the spoken answer in the chat widget, so it avoids markup and tabular layout.
/// Supports both English and Bengali with proper localized numeral conversion.
/// </summary>
public static class BookStatsFormatter
{
    public static string Describe(BookQuestion question, IReadOnlyList<BookCopyCount> counts, bool isBengali = false)
    {
        if (counts.Count == 0)
        {
            return isBengali
                ? $"{DescribeFilter(question.Filter, true)}-এর সাথে মিল রেখে কোনো বই খুঁজে পাওয়া যায়নি।"
                : $"I couldn't find any book matching {DescribeFilter(question.Filter, false)}.";
        }

        var total = counts.Sum(c => c.TotalCopies);
        var available = counts.Sum(c => c.AvailableCopies);
        var borrowed = counts.Sum(c => c.BorrowedCopies);

        if (isBengali)
        {
            var filterDesc = DescribeFilter(question.Filter, true);
            var headlineBn = question.Metric switch
            {
                CopyMetric.Borrowed => $"{filterDesc}-এর {BengaliNumberHelper.FormatCount(borrowed, true, "copy", "copies", "টি কপি")} বর্তমানে ধার দেওয়া রয়েছে।",
                CopyMetric.Available => $"{filterDesc}-এর {BengaliNumberHelper.FormatCount(available, true, "copy", "copies", "টি কপি")} বর্তমানে উপলব্ধ রয়েছে।",
                _ => $"গ্রন্থাগারে {filterDesc}-এর মোট {BengaliNumberHelper.FormatCount(total, true, "copy", "copies", "টি কপি")} রয়েছে।",
            };

            var linesBn = counts.Select(c =>
                $"\"{c.Title}\" (লেখক: {c.Author}{Detail(c)}): মোট {BengaliNumberHelper.ToBanglaDigits(c.TotalCopies)}টি, উপলব্ধ {BengaliNumberHelper.ToBanglaDigits(c.AvailableCopies)}টি, ধার দেওয়া {BengaliNumberHelper.ToBanglaDigits(c.BorrowedCopies)}টি।");

            return counts.Count == 1
                ? $"{headlineBn}\n{linesBn.First()}"
                : $"{headlineBn} মিল থাকা {BengaliNumberHelper.ToBanglaDigits(counts.Count)}টি বইয়ের মধ্যে: মোট {BengaliNumberHelper.ToBanglaDigits(total)}টি, উপলব্ধ {BengaliNumberHelper.ToBanglaDigits(available)}টি, ধার দেওয়া {BengaliNumberHelper.ToBanglaDigits(borrowed)}টি।\n{string.Join("\n", linesBn)}";
        }

        var headline = question.Metric switch
        {
            CopyMetric.Borrowed => $"{Copies(borrowed)} of {DescribeFilter(question.Filter, false)} {(borrowed == 1 ? "is" : "are")} currently borrowed.",
            CopyMetric.Available => $"{Copies(available)} of {DescribeFilter(question.Filter, false)} {(available == 1 ? "is" : "are")} available.",
            _ => $"The library has {Copies(total)} of {DescribeFilter(question.Filter, false)}.",
        };

        var lines = counts.Select(c =>
            $"\"{c.Title}\" by {c.Author}{Detail(c)}: {c.TotalCopies} total, {c.AvailableCopies} available, {c.BorrowedCopies} borrowed.");

        return counts.Count == 1
            ? $"{headline}\n{lines.First()}"
            : $"{headline} Across {counts.Count} matching books: {total} total, {available} available, {borrowed} borrowed.\n{string.Join("\n", lines)}";
    }

    public static string DescribeFilter(BookFilter filter, bool isBengali = false)
    {
        var parts = new List<string>();
        if (filter.Title is not null) parts.Add($"\"{filter.Title}\"");
        if (filter.Edition is not null) parts.Add(isBengali ? $"সংস্করণ {filter.Edition}" : $"edition {filter.Edition}");
        if (filter.Author is not null) parts.Add(isBengali ? $"লেখক {filter.Author}" : $"author {filter.Author}");
        if (filter.Publisher is not null) parts.Add(isBengali ? $"প্রকাশক {filter.Publisher}" : $"publisher {filter.Publisher}");
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
