namespace CasCap.Samples;

/// <summary>Verifies the API and consumes the configured account's receive stream.</summary>
public sealed partial class SignalCliWorker(
    ILogger<SignalCliWorker> logger,
    ISignalCliClient signalCliClient,
    ISignalCliReceiver signalCliReceiver) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var about = await signalCliClient.GetAbout(stoppingToken);
        if (about is null)
            throw new InvalidOperationException("The signal-cli REST API did not return service metadata.");

        LogConnected(logger, about.Version, about.Build, about.Mode ?? "unknown");

        await foreach (var message in signalCliReceiver.StreamMessagesAsync(stoppingToken))
            await LogMessageAsync(message, stoppingToken);
    }

    private async Task LogMessageAsync(SignalReceivedMessage message, CancellationToken cancellationToken)
    {
        var dataMessage = message.Envelope.DataMessage ?? message.Envelope.SyncMessage?.SentMessage;
        var attachments = dataMessage?.Attachments ?? [];
        LogMessageReceived(logger, message.Envelope.EnvelopeType, dataMessage is not null,
            dataMessage?.Message is not null, attachments.Length);

        foreach (var attachment in attachments)
        {
            LogAttachmentMetadata(logger, attachment.ContentType ?? "unknown", attachment.Filename is not null,
                attachment.Size ?? -1, attachment.Id is not null);

            if (string.IsNullOrWhiteSpace(attachment.Id))
                continue;

            var content = await signalCliClient.GetAttachment(attachment.Id, cancellationToken);
            LogAttachmentDownloaded(logger, content?.Length ?? 0, content is not null);
        }
    }

    [LoggerMessage(LogLevel.Information,
        "{ClassName} connected to signal-cli REST API version={Version}, build={Build}, mode={Mode}")]
    private static partial void LogConnected(ILogger logger, string version, int build, string mode,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} received a Signal envelope type={EnvelopeType}, hasDataMessage={HasDataMessage}, hasText={HasText}, attachmentCount={AttachmentCount}")]
    private static partial void LogMessageReceived(ILogger logger, string envelopeType, bool hasDataMessage,
        bool hasText, int attachmentCount, string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} attachment metadata contentType={ContentType}, hasFilename={HasFilename}, declaredBytes={DeclaredBytes}, hasId={HasId}")]
    private static partial void LogAttachmentMetadata(ILogger logger, string contentType, bool hasFilename,
        long declaredBytes, bool hasId, string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information,
        "{ClassName} downloaded attachment bytes={DownloadedBytes}, found={Found}")]
    private static partial void LogAttachmentDownloaded(ILogger logger, int downloadedBytes, bool found,
        string className = nameof(SignalCliWorker));
}
