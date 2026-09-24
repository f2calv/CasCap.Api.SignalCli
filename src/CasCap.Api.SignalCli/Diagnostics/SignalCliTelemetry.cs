using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CasCap.Diagnostics;

/// <summary>OpenTelemetry-compatible diagnostic sources emitted by the SignalCli client.</summary>
public static class SignalCliTelemetry
{
    /// <summary>Meter name hosts register with their OpenTelemetry provider.</summary>
    public const string MeterName = "CasCap.Api.SignalCli";

    /// <summary>Activity source name hosts register with their OpenTelemetry provider.</summary>
    public const string ActivitySourceName = MeterName;

    private static readonly Meter Meter = new(MeterName);

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    internal static readonly UpDownCounter<long> BufferedMessages =
        Meter.CreateUpDownCounter<long>("signalcli.receive.buffered_messages", description: "Messages buffered for consumers.");
    internal static readonly Counter<long> ConnectionAttempts =
        Meter.CreateCounter<long>("signalcli.receive.connection_attempts", description: "WebSocket connection attempts.");
    internal static readonly Counter<long> Frames =
        Meter.CreateCounter<long>("signalcli.receive.frames", description: "WebSocket frames by decode outcome.");
    internal static readonly Counter<long> Reconnects =
        Meter.CreateCounter<long>("signalcli.receive.reconnections", description: "WebSocket reconnection attempts.");
    internal static readonly Counter<long> StaleStreams =
        Meter.CreateCounter<long>("signalcli.receive.stale_streams", description: "Receive streams aborted after becoming stale.");
}
