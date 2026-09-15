using System.ComponentModel.DataAnnotations;

namespace CasCap.Samples;

/// <summary>Options for the manual Signal voice-note speech-to-text acceptance harness.</summary>
/// <remarks>
/// Sample-only, and never part of the published library. The tracked <c>appsettings.json</c> keeps the harness
/// disabled with synthetic localhost defaults; operator values belong in .NET User Secrets.
/// Used by <see cref="CasCap.Samples.SignalCliWorker"/>, <see cref="CasCap.Samples.WhisperCppVoiceTranscriber"/>
/// and <see cref="CasCap.Samples.FfmpegAudioTranscoder"/>.
/// </remarks>
public sealed record VoiceSttHarnessConfig
{
    /// <summary>The configuration section that binds to this record.</summary>
    public const string ConfigurationSectionName = "VoiceSttHarness";

    /// <summary>The name of the <see cref="HttpClient"/> registration shared by every transcriber.</summary>
    public const string HttpClientName = "VoiceStt";

    /// <summary>Whether received audio attachments are downloaded, transcoded and transcribed.</summary>
    /// <remarks>Defaults to <see langword="false"/> so a clone never touches attachments or an external endpoint.</remarks>
    public bool Enabled { get; init; }

    /// <summary>The speech-to-text server this harness talks to.</summary>
    /// <remarks>Defaults to <see cref="VoiceSttProvider.WhisperAsr"/>, the deployed service.</remarks>
    public VoiceSttProvider Provider { get; init; } = VoiceSttProvider.WhisperAsr;

    /// <summary>
    /// An audio file transcoded and transcribed once at start-up, instead of waiting for a Signal message.
    /// </summary>
    /// <remarks>
    /// File mode never contacts signal-cli, so it proves ffmpeg and the speech-to-text contract without a
    /// second Signal registration. The host stops once the file has been processed.
    /// </remarks>
    public string? AudioFilePath { get; init; }

    /// <summary>The base address of the speech-to-text server (e.g. <c>"http://localhost:8081"</c>).</summary>
    [Required, Url]
    public string Endpoint { get; init; } = "http://localhost:8081";

    /// <summary>The request path appended to <see cref="Endpoint"/>.</summary>
    /// <remarks>When empty the transcriber's own default route is used.</remarks>
    public string? RequestPath { get; init; }

    /// <summary>The language hint sent with the transcription request.</summary>
    /// <remarks>Defaults to <c>"en"</c>.</remarks>
    [Required]
    public string Language { get; init; } = "en";

    /// <summary>The largest declared attachment size, in bytes, the harness will download.</summary>
    /// <remarks>Defaults to 26 214 400 bytes (25 MiB).</remarks>
    [Range(1, long.MaxValue)]
    public long MaxAttachmentBytes { get; init; } = 26_214_400;

    /// <summary>Per-request timeout in milliseconds for the transcription call.</summary>
    /// <remarks>
    /// Defaults to 120 000 ms (2 minutes) because CPU transcription is slow.
    /// Used by <see cref="CasCap.Samples.WhisperCppVoiceTranscriber"/>.
    /// </remarks>
    [Range(1, int.MaxValue)]
    public int TimeoutMs { get; init; } = 120_000;

    /// <summary>The ffmpeg executable name or absolute path.</summary>
    /// <remarks>Defaults to <c>"ffmpeg"</c>, resolved through <c>PATH</c>.</remarks>
    [Required]
    public string FfmpegPath { get; init; } = "ffmpeg";

    /// <summary>Whether the downloaded audio is piped through ffmpeg before transcription.</summary>
    /// <remarks>
    /// Defaults to <see langword="true"/> because Android voice notes are AAC and a stock whisper.cpp server
    /// accepts WAV unless it was started with <c>--convert</c>.
    /// </remarks>
    public bool TranscodeToWav { get; init; } = true;

    /// <summary>Whether the attachment is deleted from the signal-cli wrapper after processing.</summary>
    /// <remarks>Defaults to <see langword="true"/>; the wrapper otherwise retains it on its persistent volume.</remarks>
    public bool DeleteAttachmentAfterProcessing { get; init; } = true;

    /// <summary>Whether the host stops after the first accepted voice note.</summary>
    /// <remarks>Defaults to <see langword="false"/>, which keeps the sample listening for further messages.</remarks>
    public bool StopAfterFirstAccepted { get; init; }

    /// <summary>
    /// Optional lowercase hexadecimal SHA-256 of the expected normalized transcript, never the phrase itself.
    /// </summary>
    /// <remarks>Supply through User Secrets. When unset the harness only requires a non-empty transcript.</remarks>
    public string? ExpectedTranscriptSha256 { get; init; }

    /// <summary>Combines <see cref="Endpoint"/> with <see cref="RequestPath"/>, or with a transcriber default.</summary>
    /// <param name="defaultRequestPath">The route to use when <see cref="RequestPath"/> is unset.</param>
    public string ResolveRequestUri(string defaultRequestPath)
    {
        var path = string.IsNullOrWhiteSpace(RequestPath) ? defaultRequestPath : RequestPath;
        return $"{Endpoint.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
