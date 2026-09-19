namespace Library.Application.Features.Assistant;

/// <summary>
/// Provider-agnostic description of the tools an LLM may call, plus the
/// dispatcher that actually executes one against <see cref="LibraryQueryTools"/>.
/// Anthropic/OpenAI each translate <see cref="Definitions"/> into their own
/// wire format (see AnthropicChatProvider/OpenAiChatProvider) - the tools
/// themselves, and what they are allowed to touch, are defined exactly once
/// here.
/// </summary>
public sealed class ToolCatalog(LibraryQueryTools tools)
{
    public sealed record ToolDefinition(string Name, string Description, IReadOnlyDictionary<string, ToolParam> Parameters, IReadOnlyList<string> Required);

    public sealed record ToolParam(string Type, string Description);

    public static readonly IReadOnlyList<ToolDefinition> Definitions =
    [
        new ToolDefinition(
            "get_copy_count",
            "Get how many total, available and currently borrowed physical copies the library has. Filter by any combination of book title, author, publisher and edition; all supplied filters must match. Use 'query' for a loose title/author/ISBN fragment.",
            new Dictionary<string, ToolParam>
            {
                ["title"] = new("string", "Book title (or a fragment of it)."),
                ["author"] = new("string", "Author name (or a fragment of it)."),
                ["publisher"] = new("string", "Publisher name (or a fragment of it)."),
                ["edition"] = new("string", "Edition, e.g. '2nd', 'second' or '20th Anniversary'."),
                ["query"] = new("string", "Loose title, author or ISBN fragment when the kind of value is unclear."),
            },
            []),
        new ToolDefinition(
            "most_borrowed_books",
            "Get the most-borrowed books in the library within a recent time window.",
            new Dictionary<string, ToolParam>
            {
                ["days"] = new("integer", "How many days back to look. Defaults to 30 (about a month)."),
                ["top"] = new("integer", "How many results to return. Defaults to 5."),
            },
            []),
        new ToolDefinition(
            "top_borrowers",
            "Get the members who have borrowed the most books within a recent time window.",
            new Dictionary<string, ToolParam>
            {
                ["days"] = new("integer", "How many days back to look. Defaults to 30 (about a month)."),
                ["top"] = new("integer", "How many results to return. Defaults to 5."),
            },
            []),
    ];

    /// <summary>Executes one named tool with loosely-typed JSON-decoded arguments and returns a plain-text result for the model.</summary>
    public async Task<string> ExecuteAsync(string name, IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken)
    {
        switch (name)
        {
            case "get_copy_count":
            {
                var filter = new BookFilter(
                    Text(args, "title"), Text(args, "author"), Text(args, "publisher"), Text(args, "edition"), Text(args, "query"));
                var results = await tools.GetCopyCountAsync(filter, cancellationToken);
                return results.Count == 0
                    ? $"No book found matching {BookStatsFormatter.DescribeFilter(filter)}."
                    : string.Join("; ", results.Select(r =>
                        $"\"{r.Title}\" by {r.Author} ({r.Publisher}{(r.Edition is null ? "" : ", " + r.Edition)}): {r.TotalCopies} total, {r.AvailableCopies} available, {r.BorrowedCopies} borrowed"));
            }
            case "most_borrowed_books":
            {
                var days = ToInt(args.GetValueOrDefault("days"), 30);
                var top = ToInt(args.GetValueOrDefault("top"), 5);
                var results = await tools.GetMostBorrowedBooksAsync(days, top, cancellationToken);
                return results.Count == 0
                    ? $"No borrows in the last {days} days."
                    : string.Join("; ", results.Select((r, i) => $"{i + 1}. \"{r.Title}\" by {r.Author} - {r.BorrowCount} borrows"));
            }
            case "top_borrowers":
            {
                var days = ToInt(args.GetValueOrDefault("days"), 30);
                var top = ToInt(args.GetValueOrDefault("top"), 5);
                var results = await tools.GetTopBorrowersAsync(days, top, cancellationToken);
                return results.Count == 0
                    ? $"No borrows in the last {days} days."
                    : string.Join("; ", results.Select((r, i) => $"{i + 1}. {r.Name} ({r.MembershipNumber}) - {r.BorrowCount} borrows"));
            }
            default:
                return $"Unknown tool '{name}'.";
        }
    }

    private static string? Text(IReadOnlyDictionary<string, object?> args, string key) =>
        args.GetValueOrDefault(key)?.ToString() is { Length: > 0 } value ? value : null;

    private static int ToInt(object? value, int fallback)
    {
        if (value is null) return fallback;
        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            System.Text.Json.JsonElement je when je.TryGetInt32(out var n) => n,
            string s when int.TryParse(s, out var n) => n,
            _ => fallback,
        };
    }
}
