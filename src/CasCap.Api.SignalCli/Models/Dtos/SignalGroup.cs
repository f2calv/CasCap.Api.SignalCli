namespace CasCap.Models.Dtos;

/// <summary>
/// Represents a Signal group returned by the <c>GET /v1/groups/{number}</c> or
/// <c>GET /v1/groups/{number}/{groupId}</c> endpoint.
/// </summary>
public sealed record SignalGroup : INotificationGroup
{
    /// <summary>The <c>group.</c>-prefixed identifier required when sending to this group.</summary>
    /// <remarks>
    /// Inbound messages carry <see cref="InternalId"/> instead. Use <see cref="Matches(string?)"/>
    /// when the identifier's source is not known.
    /// </remarks>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The display name of the group.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Optional group description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Phone numbers of current group members.
    /// </summary>
    [JsonPropertyName("members")]
    public string[] Members { get; init; } = [];

    /// <summary>
    /// Phone numbers of group admins.
    /// </summary>
    [JsonPropertyName("admins")]
    public string[] Admins { get; init; } = [];

    /// <summary>
    /// Whether the group is blocked.
    /// </summary>
    [JsonPropertyName("blocked")]
    public bool Blocked { get; init; }

    /// <summary>The unprefixed internal identifier carried by inbound message envelopes.</summary>
    /// <remarks>
    /// This value is not interchangeable with <see cref="Id"/> and must not be derived by removing
    /// the <c>group.</c> prefix.
    /// </remarks>
    [JsonPropertyName("internal_id")]
    public string? InternalId { get; init; }

    /// <summary>
    /// The group invite link.
    /// </summary>
    [JsonPropertyName("invite_link")]
    public string? InviteLink { get; init; }

    /// <summary>
    /// Phone numbers of pending invited members.
    /// </summary>
    [JsonPropertyName("pending_invites")]
    public string[] PendingInvites { get; init; } = [];

    /// <summary>
    /// Phone numbers of members requesting to join.
    /// </summary>
    [JsonPropertyName("pending_requests")]
    public string[] PendingRequests { get; init; } = [];

    /// <summary>
    /// Group permission settings.
    /// </summary>
    [JsonPropertyName("permissions")]
    public GroupPermissions? Permissions { get; init; }

    /// <summary>Whether <paramref name="groupId"/> is either identifier for this group.</summary>
    /// <param name="groupId">An opaque identifier from a group response or inbound envelope.</param>
    /// <returns><see langword="true"/> when the identifier matches this group.</returns>
    public bool Matches(string? groupId) =>
        groupId is not null
        && (string.Equals(groupId, Id, StringComparison.Ordinal)
            || string.Equals(groupId, InternalId, StringComparison.Ordinal));
}
