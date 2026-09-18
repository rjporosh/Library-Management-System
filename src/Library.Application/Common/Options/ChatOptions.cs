namespace Library.Application.Common.Options;

/// <summary>
/// Bound from the "Chat" config section. Feature-flagged and provider-
/// switchable by configuration only, matching the DatabaseOptions/JwtOptions
/// convention used everywhere else in this codebase. Missing API keys never
/// prevent the application from starting - an unconfigured provider simply
/// returns a clear "not configured" answer at request time (see
/// ChatService), the same fail-soft philosophy as the rest of this feature.
/// </summary>
public sealed class ChatOptions
{
    /// <summary>Master switch for the assistant. When false, the chat endpoint returns 403 FEATURE_DISABLED.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Which engine answers questions: RuleBased (default, no external dependency), Anthropic, or OpenAI.</summary>
    public string Provider { get; set; } = "RuleBased";

    public AnthropicChatOptions Anthropic { get; set; } = new();

    public OpenAiChatOptions OpenAI { get; set; } = new();
}

public sealed class AnthropicChatOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-3-5-haiku-20241022";
}

public sealed class OpenAiChatOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";
}
