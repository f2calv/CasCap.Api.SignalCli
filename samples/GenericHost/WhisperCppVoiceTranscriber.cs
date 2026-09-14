using System.Net.Http.Headers;
using System.Text.Json;

namespace CasCap.Samples;

/// <summary>Posts audio to a whisper.cpp server as multipart form data and returns the transcript.</summary>
/// <remarks>
/// Sample-only acceptance code. The request targets <c>{Endpoint}{RequestPath}</c> with the <c>file</c>,
/// <c>language</c> and <c>response_format</c> parts the stock whisper.cpp server expects. The transcript is
/// returned to the caller and never logged.
/// </remarks>
public sealed partial class WhisperCppVoiceTranscriber(
    ILogger<WhisperCppVoiceTranscriber> logger,
    IOptions<VoiceSttHarnessConfig> options,
    IHttpClientFactory httpClientFactory) : IVoiceTranscriber
{
    /// <summary>The name of the <see cref="HttpClient"/> registration used by this transcriber.</summary>
    public const string HttpClientName = nameof(WhisperCppVoiceTranscriber);

    /// <inheritdoc/>
    public async Task<string?> Transcribe(byte[] audio, string contentType, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        var requestUri = new Uri($"{config.Endpoint.TrimEnd('/')}/{config.RequestPath.TrimStart('/')}");

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(audio);
        if (MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            file.Headers.ContentType = mediaType;
        //A synthetic part filename, so a sender-supplied filename can never reach the transcription endpoint.
        content.Add(file, "file", SyntheticFileName(contentType));
        content.Add(new StringContent(config.Language), "language");
        content.Add(new StringContent("json"), "response_format");

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.PostAsync(requestUri, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            LogTranscriptionRejected(logger, (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<WhisperCppTranscriptionResponse>(stream,
            cancellationToken: cancellationToken);
        return payload?.Text;
    }

    #region Private helpers

    private static string SyntheticFileName(string contentType) => contentType switch
    {
        "audio/wav" or "audio/x-wav" or "audio/wave" => "audio.wav",
        "audio/aac" => "audio.aac",
        "audio/mpeg" => "audio.mp3",
        "audio/mp4" or "audio/m4a" => "audio.m4a",
        "audio/ogg" => "audio.ogg",
        _ => "audio.bin"
    };

    [LoggerMessage(LogLevel.Error, "{ClassName} transcription endpoint returned StatusCode={StatusCode}")]
    private static partial void LogTranscriptionRejected(ILogger logger, int statusCode,
        string className = nameof(WhisperCppVoiceTranscriber));

    #endregion
}
