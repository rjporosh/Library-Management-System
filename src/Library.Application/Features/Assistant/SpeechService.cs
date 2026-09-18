using System.Net.Http.Json;
using Library.Application.Common.Errors;
using Library.Application.Common.Options;
using Library.Application.Common.Results;

namespace Library.Application.Features.Assistant;

public sealed record TranscriptionResult(string Text);

/// <summary>
/// Server-side speech-to-text via Hugging Face's Inference API - the
/// configurable alternative to the frontend's default Web Speech API path
/// (which needs no backend at all). Only used when Speech:Provider =
/// HuggingFace.
/// </summary>
public sealed class SpeechService(SpeechOptions options, HttpClient httpClient)
{
    public async Task<Result<TranscriptionResult>> TranscribeAsync(
        Stream audio, string contentType, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return Result.Failure<TranscriptionResult>(
                new ApiError(ErrorCodes.FeatureDisabled, "Server-side transcription is disabled.", "audio"));
        }

        if (!string.Equals(options.Provider, "HuggingFace", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<TranscriptionResult>(new ApiError(
                ErrorCodes.ValidationError,
                "Speech:Provider is not set to HuggingFace. The Web Speech API (client-side) is the " +
                "default and needs no backend call - use this endpoint only when Speech:Provider=HuggingFace.",
                "provider"));
        }

        if (string.IsNullOrWhiteSpace(options.HuggingFace.ApiKey))
        {
            return Result.Failure<TranscriptionResult>(new ApiError(
                ErrorCodes.ValidationError,
                "Speech:HuggingFace:ApiKey (or the HUGGINGFACE_API_KEY environment variable) is not configured.",
                "apiKey"));
        }

        using var content = new StreamContent(audio);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "audio/webm" : contentType);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"https://api-inference.huggingface.co/models/{options.HuggingFace.Model}")
        {
            Content = content,
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.HuggingFace.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<TranscriptionResult>(new ApiError(
                ErrorCodes.InternalError,
                $"Hugging Face transcription failed ({(int)response.StatusCode}): {Truncate(body, 300)}",
                "audio"));
        }

        var json = await response.Content.ReadFromJsonAsync<HuggingFaceTranscription>(cancellationToken: cancellationToken);
        return Result.Success(new TranscriptionResult(json?.Text?.Trim() ?? ""));
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "...";

    private sealed record HuggingFaceTranscription(string? Text);
}
