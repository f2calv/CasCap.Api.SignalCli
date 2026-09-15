namespace CasCap.Samples;

/// <summary>Audio MIME helpers shared by the harness and its transcribers.</summary>
public static class AudioMediaType
{
    /// <summary>Infers a MIME content type from a file extension.</summary>
    /// <param name="path">The audio file path.</param>
    /// <returns>The inferred content type, or <c>"application/octet-stream"</c> when unrecognized.</returns>
    public static string FromFileExtension(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".wav" => "audio/wav",
        ".aac" => "audio/aac",
        ".mp3" => "audio/mpeg",
        ".m4a" or ".mp4" => "audio/mp4",
        ".ogg" or ".oga" => "audio/ogg",
        ".opus" => "audio/opus",
        ".flac" => "audio/flac",
        _ => "application/octet-stream"
    };

    /// <summary>Returns the synthetic multipart part filename for <paramref name="contentType"/>.</summary>
    /// <remarks>
    /// Speech-to-text servers sniff the container from the part filename. A synthetic name is used so
    /// neither a sender-supplied filename nor a local filename reaches the transcription endpoint.
    /// </remarks>
    public static string ToPartFileName(string contentType) => contentType switch
    {
        "audio/wav" or "audio/x-wav" or "audio/wave" => "audio.wav",
        "audio/aac" => "audio.aac",
        "audio/mpeg" => "audio.mp3",
        "audio/mp4" or "audio/m4a" => "audio.m4a",
        "audio/ogg" => "audio.ogg",
        "audio/opus" => "audio.opus",
        "audio/flac" => "audio.flac",
        _ => "audio.bin"
    };
}
