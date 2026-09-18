namespace Library.Application.Features.Assistant;

public sealed record ChatAnswer(string Text, string Provider);

public interface IChatProvider
{
    /// <summary>The provider name reported back to the caller (e.g. "RuleBased", "Anthropic", "OpenAI").</summary>
    string Name { get; }

    Task<ChatAnswer> AskAsync(string message, CancellationToken cancellationToken = default);
}
