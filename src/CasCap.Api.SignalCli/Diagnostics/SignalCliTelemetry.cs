using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CasCap.Diagnostics;

/// <summary>OpenTelemetry-compatible diagnostic sources emitted by the SignalCli client.</summary>
public static class SignalCliTelemetry
{
    /// <summary>Stable library meter name hosts register alongside their configurable application meter.</summary>
    public const string MeterName = "CasCap.Api.SignalCli";

    /// <summary>Stable library activity-source name hosts register alongside application sources.</summary>
    public const string ActivitySourceName = MeterName;

    internal const string OutcomeTagName = "outcome";
    internal const string PhaseTagName = "phase";

    private static readonly Meter Meter = new(MeterName);

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    internal static readonly UpDownCounter<long> BufferedMessages =
        Meter.CreateUpDownCounter<long>("signalcli.receive.buffered_messages", "1", "Messages buffered for consumers.");
    internal static readonly Counter<long> ConnectionAttempts =
        Meter.CreateCounter<long>("signalcli.receive.connection_attempts", "1", "WebSocket connection attempts.");
    internal static readonly Counter<long> Frames =
        Meter.CreateCounter<long>("signalcli.receive.frames", "1", "WebSocket frames by decode outcome.");
    internal static readonly Counter<long> Reconnects =
        Meter.CreateCounter<long>("signalcli.receive.reconnections", "1", "WebSocket reconnection attempts.");
    internal static readonly Counter<long> StaleStreams =
        Meter.CreateCounter<long>("signalcli.receive.stale_streams", "1", "Receive streams aborted after becoming stale.");
}
