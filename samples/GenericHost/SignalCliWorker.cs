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
            LogMessageReceived(logger, message.Envelope.DataMessage is not null);
    }

    [LoggerMessage(LogLevel.Information,
        "{ClassName} connected to signal-cli REST API version={Version}, build={Build}, mode={Mode}")]
    private static partial void LogConnected(ILogger logger, string version, int build, string mode,
        string className = nameof(SignalCliWorker));

    [LoggerMessage(LogLevel.Information, "{ClassName} received a Signal envelope, hasDataMessage={HasDataMessage}")]
    private static partial void LogMessageReceived(ILogger logger, bool hasDataMessage,
        string className = nameof(SignalCliWorker));
}