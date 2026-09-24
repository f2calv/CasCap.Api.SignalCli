using CasCap.Diagnostics;
using System.Diagnostics.Metrics;

namespace CasCap.Tests;

/// <summary>Verifies the public telemetry contract consumed by OpenTelemetry hosts.</summary>
public sealed class SignalCliTelemetryUnitTests
{
    [Fact]
    public void Instruments_UseStableSourceCountUnitsAndDescriptions()
    {
        Instrument[] instruments =
        [
            SignalCliTelemetry.BufferedMessages,
            SignalCliTelemetry.ConnectionAttempts,
            SignalCliTelemetry.Frames,
            SignalCliTelemetry.Reconnects,
            SignalCliTelemetry.StaleStreams,
        ];

        Assert.All(instruments, instrument =>
        {
            Assert.Equal(SignalCliTelemetry.MeterName, instrument.Meter.Name);
            Assert.Equal("1", instrument.Unit);
            Assert.False(string.IsNullOrWhiteSpace(instrument.Description));
        });
        Assert.Equal(SignalCliTelemetry.MeterName, SignalCliTelemetry.ActivitySourceName);
    }
}
