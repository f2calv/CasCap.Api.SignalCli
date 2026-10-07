namespace CasCap.Abstractions;

/// <summary>
/// The full signal-cli REST API surface, as implemented by
/// <see cref="CasCap.Services.SignalCliRestClientService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Depend on this rather than the concrete client so consumers can substitute a fake in tests.
/// Every method returns <see langword="null"/> or <see langword="false"/> on failure and logs the
/// cause; failures are not thrown. The exception is caller-requested cancellation, which propagates
/// as <see cref="OperationCanceledException"/> so an abandoned call is never mistaken for an API error.
/// </para>
/// <para>
/// Registration, verification and device-linking operations are unavailable when the signal-cli
/// server runs in a <c>json-rpc</c> mode, per upstream documentation. See
/// <see href="https://bbernhard.github.io/signal-cli-rest-api/"/> for the full API specification.
/// </para>
/// </remarks>
public interface ISignalCliClient
{
    #region General

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetAbout"/>
    public Task<SignalAbout?> GetAbout(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetConfiguration"/>
    public Task<SignalConfiguration?> GetConfiguration(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SetConfiguration"/>
    public Task<bool> SetConfiguration(SignalConfiguration config, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetAccountSettings"/>
    public Task<TrustModeResponse?> GetAccountSettings(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SetAccountSettings"/>
    public Task<bool> SetAccountSettings(string number, TrustModeRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Messaging

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SendMessage"/>
    public Task<SignalMessageResponse?> SendMessage(SignalMessageRequest msg, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ReceiveMessages"/>
    public Task<SignalReceivedMessage[]?> ReceiveMessages(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ShowTypingIndicator"/>
    public Task<bool> ShowTypingIndicator(string number, string recipient, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.HideTypingIndicator"/>
    public Task<bool> HideTypingIndicator(string number, string recipient, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SendReaction"/>
    public Task<bool> SendReaction(string number, string recipient, string reaction, string targetAuthor, long timestamp, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RemoveReaction"/>
    public Task<bool> RemoveReaction(string number, string recipient, string reaction, string targetAuthor, long timestamp, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SendReceipt"/>
    public Task<bool> SendReceipt(string number, string recipient, string receiptType, long timestamp, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RemoteDelete"/>
    public Task<RemoteDeleteResponse?> RemoteDelete(string number, string recipient, long timestamp, CancellationToken cancellationToken = default);

    #endregion

    #region Registration

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RegisterNumber"/>
    public Task<bool> RegisterNumber(string number, bool useVoice = false, string? captcha = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.VerifyNumber"/>
    public Task<bool> VerifyNumber(string number, string token, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.UnregisterNumber"/>
    public Task<bool> UnregisterNumber(string number, bool deleteAccount = false, bool deleteLocalData = false, CancellationToken cancellationToken = default);

    #endregion

    #region Accounts

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ListAccounts"/>
    public Task<string[]?> ListAccounts(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SetPin"/>
    public Task<bool> SetPin(string number, string pin, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RemovePin"/>
    public Task<bool> RemovePin(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SubmitRateLimitChallenge"/>
    public Task<bool> SubmitRateLimitChallenge(string number, string challengeToken, string captcha, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.UpdateAccountSettings"/>
    public Task<bool> UpdateAccountSettings(string number, bool? discoverableByNumber = null, bool? shareNumber = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SetUsername"/>
    public Task<SetUsernameResponse?> SetUsername(string number, string username, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RemoveUsername"/>
    public Task<bool> RemoveUsername(string number, CancellationToken cancellationToken = default);

    #endregion

    #region Contacts

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ListContacts"/>
    public Task<SignalContact[]?> ListContacts(string number, bool allRecipients = false, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.UpdateContact"/>
    public Task<bool> UpdateContact(string number, string recipient, string? name = null, int? expirationInSeconds = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SyncContacts"/>
    public Task<bool> SyncContacts(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetContact"/>
    public Task<SignalContact?> GetContact(string number, string uuid, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetContactAvatar"/>
    public Task<byte[]?> GetContactAvatar(string number, string uuid, CancellationToken cancellationToken = default);

    #endregion

    #region Devices

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetQrCodeLink"/>
    public Task<byte[]?> GetQrCodeLink(string deviceName = "signal-cli-rest-api", CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetQrCodeLinkRaw"/>
    public Task<DeviceLinkUriResponse?> GetQrCodeLinkRaw(string deviceName = "signal-cli-rest-api", CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ListLinkedDevices"/>
    public Task<SignalDevice[]?> ListLinkedDevices(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.AddDevice"/>
    public Task<bool> AddDevice(string number, string uri, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RemoveLinkedDevice"/>
    public Task<bool> RemoveLinkedDevice(string number, int deviceId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.DeleteLocalAccountData"/>
    public Task<bool> DeleteLocalAccountData(string number, bool ignoreRegistered = false, CancellationToken cancellationToken = default);

    #endregion

    #region Groups

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ListGroups"/>
    public Task<SignalGroup[]?> ListGroups(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetGroup"/>
    public Task<SignalGroup?> GetGroup(string number, string groupId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.CreateGroup"/>
    public Task<CreateGroupResponse?> CreateGroup(string number, CreateGroupRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.UpdateGroup"/>
    public Task<bool> UpdateGroup(string number, string groupId, UpdateGroupRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.DeleteGroup"/>
    public Task<bool> DeleteGroup(string number, string groupId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.AddGroupMembers"/>
    public Task<bool> AddGroupMembers(string number, string groupId, string[] members, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RemoveGroupMembers"/>
    public Task<bool> RemoveGroupMembers(string number, string groupId, string[] members, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.AddGroupAdmins"/>
    public Task<bool> AddGroupAdmins(string number, string groupId, string[] admins, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.RemoveGroupAdmins"/>
    public Task<bool> RemoveGroupAdmins(string number, string groupId, string[] admins, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.JoinGroup"/>
    public Task<bool> JoinGroup(string number, string groupId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.QuitGroup"/>
    public Task<bool> QuitGroup(string number, string groupId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.BlockGroup"/>
    public Task<bool> BlockGroup(string number, string groupId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetGroupAvatar"/>
    public Task<byte[]?> GetGroupAvatar(string number, string groupId, CancellationToken cancellationToken = default);

    #endregion

    #region Identities

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ListIdentities"/>
    public Task<SignalIdentity[]?> ListIdentities(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.TrustIdentity"/>
    public Task<bool> TrustIdentity(string number, string numberToTrust, bool trustAllKnownKeys = false, string? verifiedSafetyNumber = null, CancellationToken cancellationToken = default);

    #endregion

    #region Attachments

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ListAttachments"/>
    public Task<string[]?> ListAttachments(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.GetAttachment"/>
    public Task<byte[]?> GetAttachment(string attachmentId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.DeleteAttachment"/>
    public Task<bool> DeleteAttachment(string attachmentId, CancellationToken cancellationToken = default);

    #endregion

    #region Profile

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.UpdateProfile"/>
    public Task<bool> UpdateProfile(string number, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Search

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.SearchNumbers"/>
    public Task<SearchResult[]?> SearchNumbers(string number, string[] numbers, CancellationToken cancellationToken = default);

    #endregion

    #region Sticker Packs

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ListStickerPacks"/>
    public Task<SignalStickerPack[]?> ListStickerPacks(string number, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.AddStickerPack"/>
    public Task<bool> AddStickerPack(string number, string packId, string packKey, CancellationToken cancellationToken = default);

    #endregion

    #region Polls

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.CreatePoll"/>
    public Task<CreatePollResponse?> CreatePoll(string number, CreatePollRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.ClosePoll"/>
    public Task<bool> ClosePoll(string number, ClosePollRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="CasCap.Services.SignalCliRestClientService.VotePoll"/>
    public Task<bool> VotePoll(string number, VotePollRequest request, CancellationToken cancellationToken = default);

    #endregion
}
