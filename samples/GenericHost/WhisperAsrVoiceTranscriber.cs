using System.Net.Http.Headers;
using System.Text.Json;

namespace CasCap.Samples;

/// <summary>Posts audio to an openai-whisper-asr-webservice server and returns the transcript.</summary>
/// <remarks>
/// Sample-only acceptance code. The request targets <c>{Endpoint}{RequestPath}</c> with the
/// <c>audio_file</c> part and the <c>task</c>, <c>language</c>, <c>encode</c> and <c>output</c> query
/// parameters that service expects. The transcript is returned to the caller and never logged.
/// </remarks>
public sealed partial class WhisperAsrVoiceTranscriber(
    ILogger<WhisperAsrVoiceTranscriber> logger,
    IOptions<VoiceSttHarnessConfig> options,
    IHttpClientFactory httpClientFactory) : IVoiceTranscriber
{
    /// <summary>The route used when <see cref="VoiceSttHarnessConfig.RequestPath"/> is unset.</summary>
    public const string DefaultRequestPath = "/asr";

    /// <inheritdoc/>
    public async Task<string?> Transcribe(byte[] audio, string contentType, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        //encode=false skips the server-side ffmpeg pass; the harness has already produced the WAV it wants.
        var encode = contentType != FfmpegAudioTranscoder.OutputContentType;
        var requestUri = new Uri(
            $"{config.ResolveRequestUri(DefaultRequestPath)}" +
            $"?task=transcribe&language={Uri.EscapeDataString(config.Language)}" +
            $"&encode={(encode ? "true" : "false")}&output=json");

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(audio);
        if (MediaTypeHeaderValue.TryParse(contentType, out var mediaType))
            file.Headers.ContentType = mediaType;
        content.Add(file, "audio_file", AudioMediaType.ToPartFileName(contentType));

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
        string className = nameof(WhisperAsrVoiceTranscriber));
}
