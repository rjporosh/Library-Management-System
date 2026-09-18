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
            "Get how many total and available physical copies the library has of a book, matched by title, author or ISBN fragment.",
            new Dictionary<string, ToolParam> { ["title"] = new("string", "Title, author or ISBN fragment to search for.") },
            ["title"]),
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
                var title = args.GetValueOrDefault("title")?.ToString() ?? "";
                var results = await tools.GetCopyCountAsync(title, cancellationToken);
                return results.Count == 0
                    ? $"No book found matching \"{title}\"."
                    : string.Join("; ", results.Select(r => $"\"{r.Title}\" by {r.Author}: {r.AvailableCopies}/{r.TotalCopies} copies available"));
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
