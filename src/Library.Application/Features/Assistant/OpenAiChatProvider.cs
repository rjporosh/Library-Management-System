using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Library.Application.Common.Options;

namespace Library.Application.Features.Assistant;

/// <summary>
/// Calls the OpenAI Chat Completions API with function-calling constrained
/// to <see cref="ToolCatalog"/>. Same single tool round-trip design as
/// <see cref="AnthropicChatProvider"/>.
/// </summary>
public sealed class OpenAiChatProvider(HttpClient httpClient, OpenAiChatOptions options, ToolCatalog toolCatalog) : IChatProvider
{
    public string Name => "OpenAI";

    public async Task<ChatAnswer> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new ChatAnswer(
                "The OpenAI chat provider is selected but Chat:OpenAI:ApiKey (or the OPENAI_API_KEY " +
                "environment variable) is not configured. Set it, or switch Chat:Provider back to RuleBased.",
                Name);
        }

        var tools = new JsonArray(ToolCatalog.Definitions.Select(ToOpenAiTool).ToArray());
        var messages = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = message });

        var first = await SendAsync(tools, messages, cancellationToken);
        var choice = first["choices"]![0]!["message"]!.AsObject();
        var toolCalls = choice["tool_calls"]?.AsArray();

        if (toolCalls is null || toolCalls.Count == 0)
        {
            return new ChatAnswer(choice["content"]?.GetValue<string>() ?? "", Name);
        }

        messages.Add(choice.DeepClone());

        foreach (var call in toolCalls)
        {
            var function = call!["function"]!.AsObject();
            var toolName = function["name"]!.GetValue<string>();
            var argsJson = function["arguments"]!.GetValue<string>();
            var args = ParseArguments(argsJson);
            var result = await toolCatalog.ExecuteAsync(toolName, args, cancellationToken);

            messages.Add(new JsonObject
            {
                ["role"] = "tool",
                ["tool_call_id"] = call["id"]!.GetValue<string>(),
                ["content"] = result,
            });
        }

        var second = await SendAsync(tools, messages, cancellationToken);
        var finalContent = second["choices"]![0]!["message"]!["content"]?.GetValue<string>();
        return new ChatAnswer(finalContent ?? "", Name);
    }

    private async Task<JsonObject> SendAsync(JsonArray tools, JsonArray messages, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["model"] = options.Model,
            ["tools"] = tools.DeepClone(),
            ["messages"] = messages,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(body);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
        return json ?? throw new InvalidOperationException("OpenAI API returned an empty response.");
    }

    private static JsonObject ToOpenAiTool(ToolCatalog.ToolDefinition def)
    {
        var properties = new JsonObject();
        foreach (var (key, param) in def.Parameters)
        {
            properties[key] = new JsonObject { ["type"] = param.Type, ["description"] = param.Description };
        }

        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = def.Name,
                ["description"] = def.Description,
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = new JsonArray(def.Required.Select(r => JsonValue.Create(r)).ToArray()),
                },
            },
        };
    }

    private static Dictionary<string, object?> ParseArguments(string json)
    {
        var result = new Dictionary<string, object?>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.True or JsonValueKind.False => prop.Value.GetBoolean(),
                    _ => prop.Value.ToString(),
                };
            }
        }
        catch (JsonException)
        {
            // Malformed tool-call arguments from the model - execute with no
            // arguments rather than failing the whole request; ToolCatalog
            // falls back to sensible defaults for every optional parameter.
        }

        return result;
    }
}
