using System.Net.WebSockets;

namespace CasCap.Tests.Unit;

/// <summary>
/// Self-contained unit tests for <see cref="SignalCliJsonRpcClientService"/> covering
/// WebSocket URI construction, enum membership, JSON deserialization, and DI registration.
/// </summary>
[Trait("Category", "WebSocket")]
public class SignalCliJsonRpcClientServiceUnitTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false, "localhost", "+10000000000", "ws://localhost:8080/v1/receive/%2B10000000000")]
    [InlineData(true, "signal.example.com", "+10000000001", "wss://signal.example.com:8080/v1/receive/%2B10000000001")]
    [InlineData(false, "signalcli.svc.local", "+1234", "ws://signalcli.svc.local:8080/v1/receive/%2B1234")]
    public void BuildWebSocketUri_ConstructsCorrectUri(bool secure, string host, string phoneNumber, string expectedUri)
    {
        var scheme = secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        var baseAddress = new UriBuilder(scheme, host, 8080).Uri.GetLeftPart(UriPartial.Authority);
        var uri = SignalCliJsonRpcClientService.BuildWebSocketUri(baseAddress, phoneNumber);
        output.WriteLine($"Input=({baseAddress}, {phoneNumber}) => {uri}");
        Assert.Equal(expectedUri, uri.ToString());
    }

    [Theory]
    [InlineData(false, "localhost", "+10000000000")]
    [InlineData(true, "signal.example.com", "+10000000001")]
    public void MaskPhoneNumberInUri_RemovesThePercentEncodedNumber(bool secure, string host, string phoneNumber)
    {
        //The URI percent-encodes '+' as %2B, so masking only the raw form silently leaves the number behind.
        var scheme = secure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        var baseAddress = new UriBuilder(scheme, host, 8080).Uri.GetLeftPart(UriPartial.Authority);
        var uri = SignalCliJsonRpcClientService.BuildWebSocketUri(baseAddress, phoneNumber);
        var masked = SignalCliJsonRpcClientService.MaskPhoneNumberInUri(uri, phoneNumber);

        output.WriteLine($"masked => {masked}");
        Assert.DoesNotContain(phoneNumber, masked, StringComparison.Ordinal);
        Assert.DoesNotContain(Uri.EscapeDataString(phoneNumber), masked, StringComparison.Ordinal);
        Assert.DoesNotContain(phoneNumber.TrimStart('+'), masked, StringComparison.Ordinal);
    }

    [Fact]
    public void SignalCliTransport_HasExpectedMembers()
    {
        var values = Enum.GetValues<SignalCliTransport>();
        Assert.Equal(4, values.Length);
        Assert.Contains(SignalCliTransport.Normal, values);
        Assert.Contains(SignalCliTransport.Native, values);
        Assert.Contains(SignalCliTransport.JsonRpc, values);
        Assert.Contains(SignalCliTransport.JsonRpcNative, values);
    }

    [Fact]
    public void JsonRpcNotification_WithDataMessage_DeserializesCorrectly()
    {
        const string json = """
            {
              "jsonrpc": "2.0",
              "method": "receive",
              "params": {
                "envelope": {
                "source": "+10000000000",
                "sourceNumber": "+10000000000",
                  "timestamp": 1712153610000,
                  "dataMessage": {
                    "message": "Hello from group",
                    "timestamp": 1712153610000,
                    "groupInfo": { "groupId": "abc123==" }
                  }
                },
                "account": "+10000000000"
              }
            }
            """;

        var notification = json.FromJson<SignalCliJsonRpcNotification>();

        Assert.NotNull(notification);
        Assert.Equal("2.0", notification.JsonRpc);
        Assert.Equal("receive", notification.Method);
        Assert.NotNull(notification.Params);
        Assert.Equal("+10000000000", notification.Params.Envelope.Source);
        Assert.Equal("+10000000000", notification.Params.Account);
        Assert.True(((IReceivedNotification)notification.Params).HasContent);
        Assert.Equal("Hello from group", ((IReceivedNotification)notification.Params).Message);
        Assert.Equal("abc123==", ((IReceivedNotification)notification.Params).GroupId);
    }

    [Fact]
    public void JsonRpcNotification_WithSyncSentMessage_MapsReceivedNotificationContent()
    {
        // Regression for #5: a linked device receives its account's own messages under
        // syncMessage.sentMessage rather than dataMessage.
        const string json = """
            {
              "jsonrpc": "2.0",
              "method": "receive",
              "params": {
                "envelope": {
                  "timestamp": 1712153610000,
                  "syncMessage": {
                    "sentMessage": {
                      "message": "Hello from linked account",
                      "timestamp": 1712153610001,
                      "groupInfo": { "groupId": "synthetic-internal-id" },
                      "attachments": [
                        {
                          "contentType": "text/plain",
                          "id": "synthetic-attachment-id",
                          "size": 12
                        }
                      ]
                    }
                  }
                },
                "account": "+10000000000"
              }
            }
            """;

        var notification = json.FromJson<SignalCliJsonRpcNotification>();

        Assert.NotNull(notification);
        Assert.NotNull(notification.Params);
        var received = Assert.IsAssignableFrom<IReceivedNotification>(notification.Params);
        Assert.True(received.HasContent);
        Assert.Equal("Hello from linked account", received.Message);
        Assert.Equal("synthetic-internal-id", received.GroupId);
        Assert.Equal(1712153610001L, received.Timestamp);
        Assert.NotNull(received.Attachments);
        Assert.Single(received.Attachments);
    }

    [Fact]
    public void JsonRpcNotification_WithAudioAttachment_DeserializesAttachmentMetadata()
    {
        //A redacted, attachment-only voice-note shape: synthetic identifiers, no message body, no filename.
        const string json = """
            {
              "jsonrpc": "2.0",
              "method": "receive",
              "params": {
                "envelope": {
                  "source": "+10000000000",
                  "timestamp": 1712153610000,
                  "dataMessage": {
                    "timestamp": 1712153610000,
                    "attachments": [
                      {
                        "contentType": "audio/aac",
                        "id": "synthetic-attachment-id",
                        "size": 12345
                      }
                    ]
                  }
                },
                "account": "+10000000000"
              }
            }
            """;

        var notification = json.FromJson<SignalCliJsonRpcNotification>();

        Assert.NotNull(notification);
        Assert.NotNull(notification.Params);
        var dataMessage = notification.Params.Envelope.DataMessage;
        Assert.NotNull(dataMessage);
        Assert.Null(dataMessage.Message);
        Assert.NotNull(dataMessage.Attachments);
        var attachment = Assert.Single(dataMessage.Attachments);
        Assert.Equal("synthetic-attachment-id", attachment.Id);
        Assert.Equal("audio/aac", attachment.ContentType);
        Assert.Null(attachment.Filename);
        Assert.Equal(12345L, attachment.Size);
        output.WriteLine(
            $"contentType={attachment.ContentType}, hasFilename={attachment.Filename is not null}, size={attachment.Size}");
    }

    [Fact]
    public void JsonRpcNotification_WithNullParams_DoesNotThrow()
    {
        const string json = """{"jsonrpc":"2.0","method":"receive"}""";

        var notification = json.FromJson<SignalCliJsonRpcNotification>();

        Assert.NotNull(notification);
        Assert.Null(notification.Params);
    }

    [Fact]
    public void AddSignalCli_WithRestTransport_RegistersRestClient()
    {
        var configuration = BuildConfiguration(SignalCliTransport.Normal);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);
        services.AddSignalCli(configuration);

        using var sp = services.BuildServiceProvider();
        var notifier = sp.GetRequiredService<INotifier>();
        Assert.IsType<SignalCliRestClientService>(notifier);
        output.WriteLine($"INotifier resolved to {notifier.GetType().Name}");
    }

    [Fact]
    public async Task AddSignalCli_WithJsonRpcTransport_RegistersJsonRpcClient()
    {
        var configuration = BuildConfiguration(SignalCliTransport.JsonRpc);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);
        services.AddSignalCli(configuration);

        await using var sp = services.BuildServiceProvider();
        var notifier = sp.GetRequiredService<INotifier>();
        Assert.IsType<SignalCliJsonRpcClientService>(notifier);
        output.WriteLine($"INotifier resolved to {notifier.GetType().Name}");
    }

    [Fact]
    public void AddSignalCli_WithJsonRpcTransport_RestClientAlsoResolvable()
    {
        var configuration = BuildConfiguration(SignalCliTransport.JsonRpc);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);
        services.AddSignalCli(configuration);

        using var sp = services.BuildServiceProvider();
        var restClient = sp.GetRequiredService<SignalCliRestClientService>();
        Assert.NotNull(restClient);
        output.WriteLine($"SignalCliRestClientService is directly resolvable alongside JsonRpc INotifier");
    }

    #region Private helpers

    private static IConfigurationRoot BuildConfiguration(SignalCliTransport transport) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SignalCliConfig.ConfigurationSectionName}:{nameof(SignalCliConfig.TransportMode)}"] = transport.ToString(),
                [$"{SignalCliConfig.ConfigurationSectionName}:{nameof(SignalCliConfig.BaseAddress)}"] = "http://localhost:8080",
                [$"{SignalCliConfig.ConfigurationSectionName}:{nameof(SignalCliConfig.PhoneNumber)}"] = "+10000000000",
            })
            .Build();

    #endregion
}
