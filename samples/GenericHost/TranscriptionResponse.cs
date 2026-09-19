using System.Text.Json.Serialization;

namespace CasCap.Samples;

/// <summary>The JSON body returned by a speech-to-text server.</summary>
/// <remarks>
/// Both stock whisper.cpp (<c>/inference</c>) and openai-whisper-asr-webservice (<c>/asr?output=json</c>)
/// return a top-level <c>text</c> property; any extra fields whisper-asr adds are ignored.
/// </remarks>
public sealed record TranscriptionResponse
{
    /// <summary>The transcribed text.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
