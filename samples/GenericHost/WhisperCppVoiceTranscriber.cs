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
    /// <summary>The route used when <see cref="VoiceSttHarnessConfig.RequestPath"/> is unset.</summary>
    public const string DefaultRequestPath = "/inference";

    /// <inheritdoc/>
    public async Task<string?> Transcribe(byte[] audio, string contentType, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        var requestUri = new Uri(config.ResolveRequestUri(DefaultRequestPath));

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(audio);
        if (MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            file.Headers.ContentType = mediaType;
        content.Add(file, "file", AudioMediaType.ToPartFileName(contentType));
        content.Add(new StringContent(config.Language), "language");
        content.Add(new StringContent("json"), "response_format");

        var client = httpClientFactory.CreateClient(VoiceSttHarnessConfig.HttpClientName);
        using var response = await client.PostAsync(requestUri, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            LogTranscriptionRejected(logger, (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<TranscriptionResponse>(stream,
            cancellationToken: cancellationToken);
        return payload?.Text;
    }

    [LoggerMessage(LogLevel.Error, "{ClassName} transcription endpoint returned StatusCode={StatusCode}")]
    private static partial void LogTranscriptionRejected(ILogger logger, int statusCode,
        string className = nameof(WhisperCppVoiceTranscriber));
}
