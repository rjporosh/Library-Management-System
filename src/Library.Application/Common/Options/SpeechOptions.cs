namespace Library.Application.Common.Options;

/// <summary>
/// Bound from the "Speech" config section. The frontend's Web Speech API
/// path needs no backend at all; this only configures the optional
/// server-side Hugging Face speech-to-text fallback
/// (<c>POST /api/assistant/transcribe</c>).
/// </summary>
public sealed class SpeechOptions
{
    /// <summary>Master switch for the server-side transcription endpoint.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>WebSpeech (client-side, default - this endpoint is unused) or HuggingFace.</summary>
    public string Provider { get; set; } = "WebSpeech";

    public HuggingFaceSpeechOptions HuggingFace { get; set; } = new();
}

public sealed class HuggingFaceSpeechOptions
{
    public string? ApiKey { get; set; }

    /// <summary>A Hugging Face Inference API automatic-speech-recognition model.</summary>
    public string Model { get; set; } = "openai/whisper-large-v3";
}
