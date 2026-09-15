using CasCap.Models;
using CasCap.Models.Dtos;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace CasCap.Samples;

/// <summary>
/// Verifies the API, consumes the configured account's receive stream, and optionally runs the voice
/// speech-to-text acceptance harness.
/// </summary>
/// <remarks>
/// The harness is inert unless <see cref="VoiceSttHarnessConfig.Enabled"/> is set. It never replies on Signal
/// and never emits sender identity, group information, filenames, message text, transcript text or audio bytes.
/// </remarks>
public sealed partial class SignalCliWorker(
    ILogger<SignalCliWorker> logger,
    IOptions<VoiceSttHarnessConfig> voiceSttHarnessOptions,
    IOptions<SignalCliConfig> signalCliConfig,
    IHostApplicationLifetime hostApplicationLifetime,
    ISignalCliClient signalCliClient,
    ISignalCliReceiver signalCliReceiver,
    IVoiceTranscriber voiceTranscriber,
    FfmpegAudioTranscoder audioTranscoder) : BackgroundService
{
    private const string AudioContentTypePrefix = "audio/";

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = voiceSttHarnessOptions.Value;

        if (!string.IsNullOrWhiteSpace(config.AudioFilePath))
        {
            await RunFileMode(config, stoppingToken);
            hostApplicationLifetime.StopApplication();
            return;
        }

        var about = await signalCliClient.GetAbout(stoppingToken);
        if (about is null)
            throw new InvalidOperationException("The signal-cli REST API did not return service metadata.");

        LogConnected(logger, about.Version, about.Build, about.Mode ?? "unknown");
        LogHarnessState(logger, config.Enabled, config.Provider.ToString(), config.TranscodeToWav);

        var groupId = await ResolveGroupId(config, stoppingToken);

        await foreach (var message in signalCliReceiver.StreamMessagesAsync(stoppingToken))
        {
            LogMessageReceived(logger, message.Envelope.DataMessage is not null);

            if (!config.Enabled)
                continue;

            //Every consumer of the account sees the whole stream, so ignore other deployments' groups.
            if (NormalizeGroupId(message.Envelope.DataMessage?.GroupInfo?.GroupId) != NormalizeGroupId(groupId))
                continue;

            var accepted = await TryProcessVoiceNote(message, stoppingToken);
            if (accepted && config.StopAfterFirstAccepted)
            {
                LogHarnessStopping(logger);
                hostApplicationLifetime.StopApplication();
                break;
            }
        }
    }

    #region Group scoping

    /// <summary>Resolves the configured group by name, falling back to a configured raw identifier.</summary>
    private async Task<string> ResolveGroupId(VoiceSttHarnessConfig config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.GroupName))
        {
            if (string.IsNullOrWhiteSpace(config.GroupId))
                throw new InvalidOperationException(
                    "Signal mode requires VoiceSttHarness:GroupName or VoiceSttHarness:GroupId; without one the "
                    + "harness would act on messages belonging to other consumers of this account.");

            LogGroupResolved(logger, false);
            return config.GroupId;
        }

        var groups = await signalCliClient.ListGroups(signalCliConfig.Value.PhoneNumber, cancellationToken);
        var match = groups?.FirstOrDefault(g => g.Name == config.GroupName);
        if (match?.Id is not null)
        {
            LogGroupResolved(logger, true);
            return match.Id;
        }

        if (string.IsNullOrWhiteSpace(config.GroupId))
            throw new InvalidOperationException(
                "The configured VoiceSttHarness:GroupName was not found and no GroupId fallback is configured.");

        LogGroupResolved(logger, false);
        return config.GroupId;
    }

    /// <summary>
    /// Normalizes a Signal group identifier to its raw base64 form. The groups list endpoint returns the raw
    /// key while the receive stream prefixes and double-encodes it as <c>group.{Base64(rawKey)}</c>.
    /// </summary>
    private static string? NormalizeGroupId(string? groupId)
    {
        if (groupId is null)
            return null;
        if (groupId.StartsWith("group.", StringComparison.Ordinal))
        {
            var encoded = groupId["group.".Length..];
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        return groupId;
    }

    #endregion

    #region File mode

    /// <summary>Transcodes and transcribes a local audio file, without contacting signal-cli.</summary>
    private async Task<bool> RunFileMode(VoiceSttHarnessConfig config, CancellationToken cancellationToken)
    {
        LogHarnessState(logger, config.Enabled, config.Provider.ToString(), config.TranscodeToWav);

        //The path is never logged: an operator's fixture is commonly named after the phrase it contains.
        var path = config.AudioFilePath!;
        if (!File.Exists(path))
        {
            LogAudioFileMissing(logger);
            return false;
        }

        var audio = await File.ReadAllBytesAsync(path, cancellationToken);
        if (audio.LongLength > config.MaxAttachmentBytes)
        {
            LogAttachmentTooLarge(logger, audio.LongLength, config.MaxAttachmentBytes);
            return false;
        }

        var contentType = AudioMediaType.FromFileExtension(path);
        LogFileModeStarted(logger, contentType, audio.Length,
            Convert.ToHexStringLower(SHA256.HashData(audio)));

        return await TranscribeAudio(audio, contentType, config, cancellationToken);
    }

    #endregion

    #region Voice acceptance harness

    private async Task<bool> TryProcessVoiceNote(SignalReceivedMessage message, CancellationToken cancellationToken)
    {
        var config = voiceSttHarnessOptions.Value;
        var attachments = message.Envelope.DataMessage?.Attachments;
        if (attachments is null || attachments.Length == 0)
            return false;

        var attachment = Array.Find(attachments,
            a => a.ContentType?.StartsWith(AudioContentTypePrefix, StringComparison.OrdinalIgnoreCase) == true);
        if (attachment is null)
        {
            LogNoAudioAttachment(logger, attachments.Length);
            return false;
        }

        if (string.IsNullOrWhiteSpace(attachment.Id))
        {
            LogAttachmentRejected(logger, "missing id");
            return false;
        }

        if (attachment.Size > config.MaxAttachmentBytes)
        {
            LogAttachmentTooLarge(logger, attachment.Size.Value, config.MaxAttachmentBytes);
            return false;
        }

        LogAttachmentAccepted(logger, attachment.ContentType!, attachment.Filename is not null, attachment.Size ?? -1);

        try
        {
            return await ProcessAudioAttachment(attachment.Id, attachment.ContentType!, attachment.Size ?? -1, config,
                cancellationToken);
        }
        finally
        {
            if (config.DeleteAttachmentAfterProcessing)
            {
                //Cleanup must still run during shutdown, so it deliberately does not observe the stopping token.
                var deleted = await signalCliClient.DeleteAttachment(attachment.Id, CancellationToken.None);
                LogAttachmentDeleted(logger, deleted);
            }
        }
    }

    private async Task<bool> ProcessAudioAttachment(string attachmentId, string contentType, long declaredBytes,
        VoiceSttHarnessConfig config, CancellationToken cancellationToken)
    {
        var audio = await signalCliClient.GetAttachment(attachmentId, cancellationToken);
        if (audio is null || audio.Length == 0)
        {
            LogAttachmentDownloadFailed(logger, declaredBytes);
            return false;
        }

        LogAttachmentDownloaded(logger, declaredBytes, audio.Length, Convert.ToHexStringLower(SHA256.HashData(audio)));

        return await TranscribeAudio(audio, contentType, config, cancellationToken);
    }

    /// <summary>Optionally converts the audio, transcribes it, and compares the normalized transcript hash.</summary>
    private async Task<bool> TranscribeAudio(byte[] audio, string contentType, VoiceSttHarnessConfig config,
        CancellationToken cancellationToken)
    {
        if (config.TranscodeToWav)
        {
            var wav = await audioTranscoder.ToWav(audio, cancellationToken);
            if (wav is null)
                return false;

            LogTranscoded(logger, audio.Length, wav.Length);
            audio = wav;
            contentType = FfmpegAudioTranscoder.OutputContentType;
        }

        var startedAt = Stopwatch.GetTimestamp();
        var transcript = await voiceTranscriber.Transcribe(audio, contentType, cancellationToken);
        var elapsedMs = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        var normalized = Normalize(transcript);
        if (normalized.Length == 0)
        {
            LogTranscriptEmpty(logger, elapsedMs);
            return false;
        }

        var transcriptSha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        LogTranscribed(logger, elapsedMs, normalized.Length, transcriptSha256);

        if (string.IsNullOrWhiteSpace(config.ExpectedTranscriptSha256))
            return true;

        var matched = string.Equals(transcriptSha256, config.ExpectedTranscriptSha256.Trim(),
            StringComparison.OrdinalIgnoreCase);
        LogExpectedTranscriptCompared(logger, matched);
        return matched;
    }

    /// <summary>
    /// Applies the harness normalization rule: trim, collapse internal whitespace runs to a single space,
    /// then lowercase using the invariant culture.
    /// </summary>
    private static string Normalize(string? transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var builder = new StringBuilder(transcript.Length);
        var pendingSeparator = false;
        foreach (var character in transcript.AsSpan().Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSeparator = true;
                continue;
            }

            if (pendingSeparator)
                builder.Append(' ');
            pendingSeparator = false;
            builder.Append(char.ToLowerInvariant(character));
        }
        return builder.ToString();
    }

    #endregion

    #region Logging

    [LoggerMessage(LogLevel.Information,
        "{ClassName} connected to signal-cli REST API version={Version}, build={Build}, mode={Mode}")]
    private static partial void LogConnected(ILogger logger, string version, int build, string mode,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} voice acceptance harness enabled={Enabled}, provider={Provider}, transcodeToWav={TranscodeToWav}")]
    private static partial void LogHarnessState(ILogger logger, bool enabled, string provider, bool transcodeToWav,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Error, "{ClassName} the configured VoiceSttHarness:AudioFilePath does not exist")]
    private static partial void LogAudioFileMissing(ILogger logger, string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} file mode started, contentType={ContentType}, bytes={Bytes}, sha256={Sha256}")]
    private static partial void LogFileModeStarted(ILogger logger, string contentType, int bytes, string sha256,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information, "{ClassName} received a Signal envelope, hasDataMessage={HasDataMessage}")]
    private static partial void LogMessageReceived(ILogger logger, bool hasDataMessage,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Debug, "{ClassName} no audio attachment among attachmentCount={AttachmentCount}")]
    private static partial void LogNoAudioAttachment(ILogger logger, int attachmentCount,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Warning, "{ClassName} rejected an audio attachment, reason={Reason}")]
    private static partial void LogAttachmentRejected(ILogger logger, string reason,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Warning,
        "{ClassName} rejected an audio attachment, declaredBytes={DeclaredBytes} exceeds maxAttachmentBytes={MaxAttachmentBytes}")]
    private static partial void LogAttachmentTooLarge(ILogger logger, long declaredBytes, long maxAttachmentBytes,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} accepted an audio attachment, contentType={ContentType}, hasFilename={HasFilename}, declaredBytes={DeclaredBytes}")]
    private static partial void LogAttachmentAccepted(ILogger logger, string contentType, bool hasFilename,
        long declaredBytes, string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Error, "{ClassName} attachment download returned no bytes, declaredBytes={DeclaredBytes}")]
    private static partial void LogAttachmentDownloadFailed(ILogger logger, long declaredBytes,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} downloaded an attachment, declaredBytes={DeclaredBytes}, downloadedBytes={DownloadedBytes}, sha256={Sha256}")]
    private static partial void LogAttachmentDownloaded(ILogger logger, long declaredBytes, int downloadedBytes,
        string sha256, string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} converted audio to WAV, sourceBytes={SourceBytes}, convertedBytes={ConvertedBytes}")]
    private static partial void LogTranscoded(ILogger logger, int sourceBytes, int convertedBytes,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Error, "{ClassName} transcription produced no text, elapsedMs={ElapsedMs}")]
    private static partial void LogTranscriptEmpty(ILogger logger, long elapsedMs,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} transcribed audio, elapsedMs={ElapsedMs}, transcriptChars={TranscriptChars}, transcriptSha256={TranscriptSha256}")]
    private static partial void LogTranscribed(ILogger logger, long elapsedMs, int transcriptChars,
        string transcriptSha256, string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information, "{ClassName} expectedTranscriptMatched={ExpectedTranscriptMatched}")]
    private static partial void LogExpectedTranscriptCompared(ILogger logger, bool expectedTranscriptMatched,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information, "{ClassName} attachment deletion succeeded={Deleted}")]
    private static partial void LogAttachmentDeleted(ILogger logger, bool deleted,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information, "{ClassName} stopping after the first accepted voice note")]
    private static partial void LogHarnessStopping(ILogger logger, string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information, "{ClassName} group scope resolved by name={ResolvedByName}")]
    private static partial void LogGroupResolved(ILogger logger, bool resolvedByName,
        string className = nameof(SignalCliWorker));

    #endregion
}
