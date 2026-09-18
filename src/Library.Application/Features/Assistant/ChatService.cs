using Library.Application.Common.Errors;
using Library.Application.Common.Options;
using Library.Application.Common.Results;

namespace Library.Application.Features.Assistant;

public sealed record ChatRequest(string Message);

public sealed record ChatResponse(string Answer, string Provider);

/// <summary>
/// Picks the configured chat provider (RuleBased/Anthropic/OpenAI) and asks
/// it the question. Never crashes the application over a missing API key -
/// an unconfigured LLM provider answers with a clear configuration message
/// (see AnthropicChatProvider/OpenAiChatProvider) rather than throwing.
/// </summary>
public sealed class ChatService(
    ChatOptions options,
    RuleBasedChatProvider ruleBased,
    AnthropicChatProvider anthropic,
    OpenAiChatProvider openAi)
{
    public async Task<Result<ChatResponse>> AskAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return Result.Failure<ChatResponse>(
                new ApiError(ErrorCodes.FeatureDisabled, "The chat assistant is disabled.", "message"));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Result.Failure<ChatResponse>(
                new ApiError(ErrorCodes.ValidationError, "A message is required.", "message", Required: true));
        }

        IChatProvider provider = options.Provider.Trim().ToLowerInvariant() switch
        {
            "anthropic" => anthropic,
            "openai" => openAi,
            _ => ruleBased,
        };

        var answer = await provider.AskAsync(request.Message.Trim(), cancellationToken);
        return Result.Success(new ChatResponse(answer.Text, answer.Provider));
    }
}
