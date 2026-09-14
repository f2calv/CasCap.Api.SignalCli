using System.Text.Json.Serialization;

namespace CasCap.Samples;

/// <summary>The JSON body returned by the whisper.cpp <c>/inference</c> endpoint.</summary>
public sealed record WhisperCppTranscriptionResponse
{
    /// <summary>The transcribed text.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
