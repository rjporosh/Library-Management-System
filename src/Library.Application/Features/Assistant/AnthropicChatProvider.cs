using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Library.Application.Common.Options;

namespace Library.Application.Features.Assistant;

/// <summary>
/// Calls the Anthropic Messages API with tool-use constrained to
/// <see cref="ToolCatalog"/>. A single tool round-trip: if Claude asks to
/// call tools, they're executed and the results sent back once for a final
/// answer - not a full multi-turn agent loop, but a real, working "ask a
/// question, the model can look something up, then answers" flow.
/// </summary>
public sealed class AnthropicChatProvider(HttpClient httpClient, AnthropicChatOptions options, ToolCatalog toolCatalog) : IChatProvider
{
    public string Name => "Anthropic";

    public async Task<ChatAnswer> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new ChatAnswer(
                "The Anthropic chat provider is selected but Chat:Anthropic:ApiKey (or the ANTHROPIC_API_KEY " +
                "environment variable) is not configured. Set it, or switch Chat:Provider back to RuleBased.",
                Name);
        }

        var tools = new JsonArray(ToolCatalog.Definitions.Select(ToAnthropicTool).ToArray());
        var messages = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = message });

        var first = await SendAsync(tools, messages, cancellationToken);
        var content = first["content"]!.AsArray();
        var toolUses = content.Where(c => c!["type"]!.GetValue<string>() == "tool_use").ToList();

        if (toolUses.Count == 0)
        {
            return new ChatAnswer(ExtractText(content), Name);
        }

        messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = content.DeepClone() });

        var resultsContent = new JsonArray();
        foreach (var use in toolUses)
        {
            var toolName = use!["name"]!.GetValue<string>();
            var toolId = use["id"]!.GetValue<string>();
            var input = JsonNodeToDictionary(use["input"]);
            var result = await toolCatalog.ExecuteAsync(toolName, input, cancellationToken);
            resultsContent.Add(new JsonObject
            {
                ["type"] = "tool_result",
                ["tool_use_id"] = toolId,
                ["content"] = result,
            });
        }

        messages.Add(new JsonObject { ["role"] = "user", ["content"] = resultsContent });

        var second = await SendAsync(tools, messages, cancellationToken);
        return new ChatAnswer(ExtractText(second["content"]!.AsArray()), Name);
    }

    private async Task<JsonObject> SendAsync(JsonArray tools, JsonArray messages, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["model"] = options.Model,
            ["max_tokens"] = 1024,
            ["tools"] = tools.DeepClone(),
            ["messages"] = messages,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(body);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
        return json ?? throw new InvalidOperationException("Anthropic API returned an empty response.");
    }

    private static string ExtractText(JsonArray content) =>
        string.Join("\n", content.Where(c => c!["type"]!.GetValue<string>() == "text").Select(c => c!["text"]!.GetValue<string>()));

    private static JsonObject ToAnthropicTool(ToolCatalog.ToolDefinition def)
    {
        var properties = new JsonObject();
        foreach (var (key, param) in def.Parameters)
        {
            properties[key] = new JsonObject { ["type"] = param.Type, ["description"] = param.Description };
        }

        return new JsonObject
        {
            ["name"] = def.Name,
            ["description"] = def.Description,
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new JsonArray(def.Required.Select(r => JsonValue.Create(r)).ToArray()),
            },
        };
    }

    private static Dictionary<string, object?> JsonNodeToDictionary(JsonNode? node)
    {
        var result = new Dictionary<string, object?>();
        if (node is not JsonObject obj) return result;
        foreach (var (key, value) in obj)
        {
            if (value is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue<double>(out var number)) result[key] = number;
                else if (jsonValue.TryGetValue<string>(out var str)) result[key] = str;
                else if (jsonValue.TryGetValue<bool>(out var boolean)) result[key] = boolean;
                else result[key] = value.ToString();
            }
            else
            {
                result[key] = value?.ToString();
            }
        }

        return result;
    }
}
