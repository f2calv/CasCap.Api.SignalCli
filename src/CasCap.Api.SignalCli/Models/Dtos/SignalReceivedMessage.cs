namespace CasCap.Models.Dtos;

/// <summary>
/// Represents a single message envelope returned by the <c>GET /v1/receive/{number}</c> endpoint.
/// The signal-cli REST API returns a JSON array of these objects.
/// </summary>
/// <remarks>
/// See <see href="https://bbernhard.github.io/signal-cli-rest-api/"/> for the full API specification.
/// </remarks>
public sealed record SignalReceivedMessage : IReceivedNotification
{
    private SignalDataMessage? Content => Envelope.DataMessage ?? Envelope.SyncMessage?.SentMessage;

    /// <summary>
    /// The message envelope containing source, timestamp and typed message data.
    /// </summary>
    [JsonPropertyName("envelope")]
    public required SignalEnvelope Envelope { get; init; }

    /// <summary>
    /// The account phone number that received the message.
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; init; }

    /// <inheritdoc/>
    [JsonIgnore]
    string IReceivedNotification.Sender => Envelope.Source ?? Envelope.SourceNumber ?? "unknown";

    /// <summary>The unprefixed <see cref="SignalGroup.InternalId"/> carried by an inbound envelope.</summary>
    /// <remarks>Use <see cref="SignalGroup.Matches(string?)"/> to compare it with a listed group.</remarks>
    [JsonIgnore]
    string? IReceivedNotification.GroupId => Content?.GroupInfo?.GroupId;

    /// <inheritdoc/>
    [JsonIgnore]
    string? IReceivedNotification.Message => Content?.Message;

    /// <inheritdoc/>
    [JsonIgnore]
    bool IReceivedNotification.HasContent => Content is not null;

    /// <inheritdoc/>
    [JsonIgnore]
    long? IReceivedNotification.Timestamp => Content?.Timestamp;

    /// <inheritdoc/>
    [JsonIgnore]
    IReadOnlyList<INotificationAttachment>? IReceivedNotification.Attachments => Content?.Attachments;
}
