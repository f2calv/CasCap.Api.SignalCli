using System.Net;

namespace CasCap.Tests.Unit;

/// <summary>Checks that send diagnostics exclude request text and upstream failure details.</summary>
public sealed class SignalCliSendLoggingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task SendMessage_LogsOutcomeWithoutPayload(int scenario)
    {
        const string Message = "synthetic-private-message";
        const string Failure = "synthetic-private-upstream-detail";
        using var handler = scenario switch
        {
            0 => StubHttpMessageHandler.RespondJson("""{"timestamp":"1758518400000"}"""),
            1 => StubHttpMessageHandler.RespondJson($"\"{Failure}\"", HttpStatusCode.BadRequest),
            _ => new StubHttpMessageHandler(_ => throw new HttpRequestException(Failure))
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var logger = new RecordingLogger();
        var service = new SignalCliRestClientService(logger,
            Options.Create(new SignalCliConfig
            {
                BaseAddress = "http://localhost:8080",
                PhoneNumber = "+10000000000"
            }),
            new StubHttpClientFactory(client));

        var result = await service.SendMessage(new SignalMessageRequest
        {
            Number = "+10000000000",
            Recipients = ["example-group"],
            Message = Message
        }, TestContext.Current.CancellationToken);

        Assert.Equal(scenario == 0, result is not null);
        Assert.Single(handler.Calls);
        Assert.NotEmpty(logger.Entries);
        foreach (var entry in logger.Entries)
        {
            Assert.DoesNotContain(Message, entry);
            Assert.DoesNotContain(Failure, entry);
            Assert.DoesNotContain("example-group", entry);
            Assert.DoesNotContain("+10000000000", entry);
        }
    }

    private sealed class RecordingLogger : ILogger<SignalCliRestClientService>
    {
        public List<string> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(formatter(state, exception) + exception?.ToString());
    }
}
