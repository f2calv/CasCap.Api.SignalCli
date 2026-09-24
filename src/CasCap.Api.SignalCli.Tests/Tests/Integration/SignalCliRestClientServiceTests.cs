namespace CasCap.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="SignalCliRestClientService"/> against a real signal-cli REST API instance.
/// </summary>
/// <remarks>
/// These tests require a running signal-cli REST API configured via <c>appsettings.Development.json</c>
/// with a valid <c>SignalCliConfig.BaseAddress</c> and <c>SignalCliConfig.PhoneNumber</c>.
/// </remarks>
[Trait(SignalCliTraits.Category, SignalCliTraits.Integration)]
public class SignalCliRestClientServiceTests(ITestOutputHelper output) : TestBase(output)
{
    /// <summary>Prefix used by all integration-test groups so they can be safely swept on teardown.</summary>
    private const string TestGroupPrefix = "TestGroup_";

    /// <inheritdoc/>
    /// <remarks>
    /// Sweeps any leftover <c>TestGroup_*</c> groups created by integration tests that crashed
    /// before their own cleanup ran. Only groups matching <see cref="TestGroupPrefix"/> are
    /// removed. The configured existing group is never touched.
    /// </remarks>
    public override async ValueTask DisposeAsync()
    {
        try
        {
            var groups = await _svc.ListGroups(_config.PhoneNumber);
            if (groups is not null)
            {
                foreach (var group in groups.Where(g =>
                    g.Name?.StartsWith(TestGroupPrefix, StringComparison.Ordinal) == true))
                {
                    var deleted = await _svc.DeleteGroup(_config.PhoneNumber, group.Id);
                    _output.WriteLine($"Teardown: deleted orphaned integration-test group={deleted}");
                }
            }
        }
        catch (Exception ex)
        {
            // Teardown is best-effort — never fail a test run because cleanup hit a transient error.
            _output.WriteLine($"Teardown: orphan sweep failed: {ex.Message}");
        }

        await base.DisposeAsync();
    }

    #region General

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.General)]
    public async Task GetAbout_ReturnsVersionInfo()
    {
        var result = await _svc.GetAbout(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Version));
        _output.WriteLine($"Version={result.Version}, Build={result.Build}, Mode={result.Mode}");
        if (result.Versions.Length > 0)
            _output.WriteLine($"SupportedVersions={string.Join(", ", result.Versions)}");
        if (result.Capabilities is not null)
            _output.WriteLine($"Capabilities={string.Join(", ", result.Capabilities.Keys)}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.General)]
    public async Task GetConfiguration_ReturnsConfiguration()
    {
        var result = await _svc.GetConfiguration(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Logging.Level={result.Logging?.Level ?? "(null)"}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.General)]
    public async Task SetConfiguration_ReturnsTrue()
    {
        var config = new SignalConfiguration
        {
            Logging = new LoggingConfiguration { Level = "INFO" }
        };
        var result = await _svc.SetConfiguration(config, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"SetConfiguration={result}");
    }

    #endregion

    #region Messaging

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendMessage_ToSelf_ReturnsTimestamp()
    {
        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"Integration test message sent at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber]
        };

        var result = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Timestamp));
        _output.WriteLine($"Timestamp={result.Timestamp}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendMessage_ToGroup_ReturnsTimestamp()
    {
        // Flush pending envelopes so signal-cli processes group membership
        // changes (e.g. a member accepting an invite) before we read state.
        var pending = await _svc.ReceiveMessages(_config.PhoneNumber, TestContext.Current.CancellationToken);
        _output.WriteLine($"FlushedMessages={pending?.Length ?? 0}");

        var groups = await _svc.ListGroups(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.NotNull(groups);
        var configuredGroup = groups.FirstOrDefault(g => g.Name == _groupName);
        Assert.NotNull(configuredGroup);

        _output.WriteLine($"Members={configuredGroup.Members.Length}, PendingInvites={configuredGroup.PendingInvites.Length}");

        // Trust all member identities
        // current Sender Keys for members who joined after the last session.
        foreach (var member in configuredGroup.Members.Where(m => m != _config.PhoneNumber))
        {
            var trusted = await _svc.TrustIdentity(_config.PhoneNumber, member, trustAllKnownKeys: true,
                cancellationToken: TestContext.Current.CancellationToken);
            _output.WriteLine($"TrustIdentity={trusted}");
        }
        var synced = await _svc.SyncContacts(_config.PhoneNumber, TestContext.Current.CancellationToken);
        _output.WriteLine($"SyncContacts={synced}");

        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"Group test message sent at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [configuredGroup.Id]
        };

        var result = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Timestamp));
        _output.WriteLine($"Timestamp={result.Timestamp}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendMessage_DirectToMember_ReturnsTimestamp()
    {
        // Diagnostic: test direct 1:1 delivery to the second group member
        // to isolate whether the issue is group-specific or general.
        var groups = await _svc.ListGroups(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.NotNull(groups);
        var configuredGroup = groups.FirstOrDefault(g => g.Name == _groupName);
        Assert.NotNull(configuredGroup);

        _output.WriteLine($"Members={configuredGroup.Members.Length}, PendingInvites={configuredGroup.PendingInvites.Length}");

        var otherMember = configuredGroup.Members.FirstOrDefault(m => m != _config.PhoneNumber);
        Assert.NotNull(otherMember);
        var trusted = await _svc.TrustIdentity(_config.PhoneNumber, otherMember, trustAllKnownKeys: true,
            cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"TrustIdentity={trusted}");

        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"Direct test message sent at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [otherMember]
        };

        var result = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Timestamp));
        _output.WriteLine($"Timestamp={result.Timestamp}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendMessage_WithStyledTextMode_ReturnsTimestamp()
    {
        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"*Bold* _italic_ test at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber],
            TextMode = "styled"
        };

        var result = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Timestamp={result.Timestamp}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendMessage_WithMentions_ReturnsTimestamp()
    {
        var mention = new MessageMention
        {
            Author = _config.PhoneNumber,
            Start = 0,
            Length = 5
        };

        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"@test mention test at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber],
            Mentions = [mention]
        };

        var result = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Timestamp={result.Timestamp}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendMessage_WithLinkPreview_ReturnsTimestamp()
    {
        var preview = new SignalLinkPreview
        {
            Url = "https://example.com",
            Title = "Test Link"
        };

        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"Link preview test at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber],
            LinkPreview = preview
        };

        var result = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Timestamp={result.Timestamp}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task ShowTypingIndicator_ReturnsTrue()
    {
        var result = await _svc.ShowTypingIndicator(_config.PhoneNumber, _config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine("Typing indicator shown");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task HideTypingIndicator_ReturnsTrue()
    {
        await _svc.ShowTypingIndicator(_config.PhoneNumber, _config.PhoneNumber, TestContext.Current.CancellationToken);
        var result = await _svc.HideTypingIndicator(_config.PhoneNumber, _config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine("Typing indicator hidden");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    [Trait(SignalCliTraits.Transport, SignalCliTraits.Polling)]
    public async Task ReceiveMessages_ReturnsMessages()
    {
        var result = await _svc.ReceiveMessages(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"ReceivedMessages={result.Length}");
        foreach (var msg in result)
        {
            var data = msg.Envelope.DataMessage;
            _output.WriteLine($"  HasDataMessage={data is not null}, Attachments={data?.Attachments?.Length ?? 0}");
            if (msg.Envelope.SyncMessage?.SentMessage is not null)
                _output.WriteLine("  HasSyncSentMessage=True");
            if (msg.Envelope.TypingMessage is not null)
                _output.WriteLine($"  TypingAction={msg.Envelope.TypingMessage.Action}");
            if (msg.Envelope.ReceiptMessage is not null)
                _output.WriteLine($"  ReceiptType={msg.Envelope.ReceiptMessage.Type}, "
                    + $"When={msg.Envelope.ReceiptMessage.When}");
            if (data?.GroupInfo is not null)
                _output.WriteLine($"  HasGroupInfo=True, Type={data.GroupInfo.Type}");
        }
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendReaction_ReturnsTrue()
    {
        // Send a message first to get a timestamp to react to
        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"React target at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber]
        };
        var sent = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(sent);

        var timestamp = long.Parse(sent.Timestamp);
        var result = await _svc.SendReaction(_config.PhoneNumber, _config.PhoneNumber, "\U0001F44D",
            _config.PhoneNumber, timestamp, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"SendReaction={result}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task RemoveReaction_ReturnsTrue()
    {
        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"Remove reaction target at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber]
        };
        var sent = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(sent);

        var timestamp = long.Parse(sent.Timestamp);
        await _svc.SendReaction(_config.PhoneNumber, _config.PhoneNumber, "\U0001F44D",
            _config.PhoneNumber, timestamp, TestContext.Current.CancellationToken);
        var result = await _svc.RemoveReaction(_config.PhoneNumber, _config.PhoneNumber, "\U0001F44D",
            _config.PhoneNumber, timestamp, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"RemoveReaction={result}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task SendReceipt_ReturnsTrue()
    {
        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"Receipt target at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber]
        };
        var sent = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(sent);

        var timestamp = long.Parse(sent.Timestamp);
        var result = await _svc.SendReceipt(_config.PhoneNumber, _config.PhoneNumber, "read", timestamp, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"SendReceipt={result}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Messaging)]
    public async Task RemoteDelete_ReturnsResponse()
    {
        var msg = new SignalMessageRequest
        {
            Id = Guid.NewGuid(),
            Message = $"Delete target at {DateTime.UtcNow:O}",
            Number = _config.PhoneNumber,
            Recipients = [_config.PhoneNumber]
        };
        var sent = await _svc.SendMessage(msg, TestContext.Current.CancellationToken);
        Assert.NotNull(sent);

        var timestamp = long.Parse(sent.Timestamp);
        var result = await _svc.RemoteDelete(_config.PhoneNumber, _config.PhoneNumber, timestamp, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"RemoteDelete timestamp={result.Timestamp}");
    }

    #endregion

    #region Registration

    [Fact(Skip = "RegisterNumber requires a dedicated test phone number")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Registration)]
    public async Task RegisterNumber_RequiresDedicatedTestNumber()
    {
        var result = await _svc.RegisterNumber("+10000000000", cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"RegisterNumber={result}");
    }

    [Fact(Skip = "VerifyNumber requires a dedicated test phone number and token")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Registration)]
    public async Task VerifyNumber_RequiresDedicatedTestNumber()
    {
        var result = await _svc.VerifyNumber("+10000000000", "000000", TestContext.Current.CancellationToken);
        _output.WriteLine($"VerifyNumber={result}");
    }

    [Fact(Skip = "UnregisterNumber is destructive and requires a dedicated test number")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Registration)]
    public async Task UnregisterNumber_RequiresDedicatedTestNumber()
    {
        var result = await _svc.UnregisterNumber("+10000000000", cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"UnregisterNumber={result}");
    }

    #endregion

    #region Accounts

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Accounts)]
    public async Task ListAccounts_ReturnsAccounts()
    {
        var result = await _svc.ListAccounts(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Accounts={result.Length}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Accounts)]
    public async Task SetPin_ReturnsTrue()
    {
        var result = await _svc.SetPin(_config.PhoneNumber, "123456", TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"SetPin={result}");

        // Clean up
        await _svc.RemovePin(_config.PhoneNumber, TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Accounts)]
    public async Task RemovePin_ReturnsTrue()
    {
        // Set then remove
        await _svc.SetPin(_config.PhoneNumber, "654321", TestContext.Current.CancellationToken);
        var result = await _svc.RemovePin(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"RemovePin={result}");
    }

    [Fact(Skip = "SubmitRateLimitChallenge requires a real challenge token and captcha")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Accounts)]
    public async Task SubmitRateLimitChallenge_RequiresRealToken()
    {
        var result = await _svc.SubmitRateLimitChallenge(_config.PhoneNumber, "token", "captcha", TestContext.Current.CancellationToken);
        _output.WriteLine($"SubmitRateLimitChallenge={result}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Accounts)]
    public async Task UpdateAccountSettings_ReturnsTrue()
    {
        var result = await _svc.UpdateAccountSettings(_config.PhoneNumber, discoverableByNumber: true,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"UpdateAccountSettings={result}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Accounts)]
    public async Task SetAndRemoveUsername_RoundTrip()
    {
        var setResult = await _svc.SetUsername(_config.PhoneNumber, $"testuser_{DateTime.UtcNow:yyyyMMddHHmmss}",
            TestContext.Current.CancellationToken);
        Assert.NotNull(setResult);
        Assert.False(string.IsNullOrWhiteSpace(setResult.Username));
        _output.WriteLine($"SetUsername={setResult.Username is not null}");

        var removeResult = await _svc.RemoveUsername(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.True(removeResult);
        _output.WriteLine($"RemoveUsername={removeResult}");
    }

    #endregion

    #region Contacts

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Contacts)]
    public async Task ListContacts_ReturnsContacts()
    {
        var result = await _svc.ListContacts(_config.PhoneNumber, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Contacts={result.Length}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Contacts)]
    public async Task UpdateContact_ReturnsTrue()
    {
        var result = await _svc.UpdateContact(_config.PhoneNumber, _config.PhoneNumber, name: "Test Self",
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"UpdateContact={result}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Contacts)]
    public async Task SyncContacts_ReturnsTrue()
    {
        var result = await _svc.SyncContacts(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"SyncContacts={result}");
    }

    #endregion

    #region Devices

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Devices)]
    public async Task ListLinkedDevices_ReturnsDevices()
    {
        var result = await _svc.ListLinkedDevices(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"LinkedDevices={result.Length}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Devices)]
    public async Task GetQrCodeLink_ReturnsBytesOrNull()
    {
        var bytes = await _svc.GetQrCodeLink("test-device", TestContext.Current.CancellationToken);
        _output.WriteLine(bytes is not null
            ? $"QR code size={bytes.Length} bytes"
            : "QR code endpoint returned null (expected when already linked)");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Devices)]
    public async Task GetQrCodeLinkRaw_ReturnsUriOrNull()
    {
        var result = await _svc.GetQrCodeLinkRaw("test-device-raw", TestContext.Current.CancellationToken);
        _output.WriteLine(result?.DeviceLinkUri is not null
            ? $"DeviceLinkUri={result.DeviceLinkUri}"
            : "QR code raw endpoint returned null (expected when already linked)");
    }

    [Fact(Skip = "AddDevice requires a real device-link URI from a QR code scan")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Devices)]
    public async Task AddDevice_RequiresRealUri()
    {
        var result = await _svc.AddDevice(_config.PhoneNumber, "sgnl://linkdevice?uuid=test&pub_key=test",
            TestContext.Current.CancellationToken);
        _output.WriteLine($"AddDevice={result}");
    }

    [Fact(Skip = "RemoveLinkedDevice requires a linked device to remove")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Devices)]
    public async Task RemoveLinkedDevice_RequiresSpareDevice()
    {
        var result = await _svc.RemoveLinkedDevice(_config.PhoneNumber, 99, TestContext.Current.CancellationToken);
        _output.WriteLine($"RemoveLinkedDevice={result}");
    }

    [Fact(Skip = "DeleteLocalAccountData is destructive and cannot be undone")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Devices)]
    public async Task DeleteLocalAccountData_Destructive()
    {
        var result = await _svc.DeleteLocalAccountData(_config.PhoneNumber, cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"DeleteLocalAccountData={result}");
    }

    #endregion

    #region Groups

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task ListGroups_ReturnsGroups()
    {
        var result = await _svc.ListGroups(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.All(result, group =>
        {
            Assert.True(group.Matches(group.Id));
            if (group.InternalId is not null)
                Assert.True(group.Matches(group.InternalId));
        });
        _output.WriteLine($"Groups={result.Length}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task CreateUpdateAndDeleteGroup_RoundTrip()
    {
        var groupName = $"TestGroup_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var createRequest = new CreateGroupRequest
        {
            Name = groupName,
            Members = [_config.PhoneNumber],
            Description = "Integration test group",
            Permissions = new GroupPermissions
            {
                AddMembers = "every-member",
                EditGroup = "every-member",
                SendMessages = "every-member"
            }
        };

        var created = await _svc.CreateGroup(_config.PhoneNumber, createRequest, TestContext.Current.CancellationToken);
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        _output.WriteLine("Created integration-test group");

        try
        {
            // GetGroup
            var fetched = await _svc.GetGroup(_config.PhoneNumber, created.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(fetched);
            _output.WriteLine("Fetched integration-test group");

            // UpdateGroup
            var updateRequest = new UpdateGroupRequest
            {
                Name = $"{groupName}_Updated",
                Description = "Updated description"
            };
            var updated = await _svc.UpdateGroup(_config.PhoneNumber, created.Id, updateRequest, TestContext.Current.CancellationToken);
            _output.WriteLine($"UpdateGroup={updated}");
        }
        finally
        {
            // Deterministic teardown — always delete the group even if an assertion
            // above fails mid-test, so we never leak orphaned TestGroup_* groups.
            var deleted = await _svc.DeleteGroup(_config.PhoneNumber, created.Id, TestContext.Current.CancellationToken);
            Assert.True(deleted);
            _output.WriteLine("Deleted integration-test group");
        }
    }

    [Fact(Skip = "AddGroupMembers/RemoveGroupMembers require a second phone number")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task AddAndRemoveGroupMembers_RequiresSecondNumber()
    {
        var added = await _svc.AddGroupMembers(_config.PhoneNumber, "group-id", ["+10000000001"], TestContext.Current.CancellationToken);
        _output.WriteLine($"AddGroupMembers={added}");
        var removed = await _svc.RemoveGroupMembers(_config.PhoneNumber, "group-id", ["+10000000001"], TestContext.Current.CancellationToken);
        _output.WriteLine($"RemoveGroupMembers={removed}");
    }

    [Fact(Skip = "AddGroupAdmins/RemoveGroupAdmins require a second phone number")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task AddAndRemoveGroupAdmins_RequiresSecondNumber()
    {
        var added = await _svc.AddGroupAdmins(_config.PhoneNumber, "group-id", ["+10000000001"], TestContext.Current.CancellationToken);
        _output.WriteLine($"AddGroupAdmins={added}");
        var removed = await _svc.RemoveGroupAdmins(_config.PhoneNumber, "group-id", ["+10000000001"], TestContext.Current.CancellationToken);
        _output.WriteLine($"RemoveGroupAdmins={removed}");
    }

    [Fact(Skip = "JoinGroup requires a group invite link")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task JoinGroup_RequiresInviteLink()
    {
        var result = await _svc.JoinGroup(_config.PhoneNumber, "group-id", TestContext.Current.CancellationToken);
        _output.WriteLine($"JoinGroup={result}");
    }

    [Fact(Skip = "QuitGroup is destructive")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task QuitGroup_Destructive()
    {
        var result = await _svc.QuitGroup(_config.PhoneNumber, "group-id", TestContext.Current.CancellationToken);
        _output.WriteLine($"QuitGroup={result}");
    }

    [Fact(Skip = "BlockGroup is destructive")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task BlockGroup_Destructive()
    {
        var result = await _svc.BlockGroup(_config.PhoneNumber, "group-id", TestContext.Current.CancellationToken);
        _output.WriteLine($"BlockGroup={result}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Groups)]
    public async Task GetGroupAvatar_ReturnsNullForNoAvatar()
    {
        var groups = await _svc.ListGroups(_config.PhoneNumber, TestContext.Current.CancellationToken);
        if (groups is null || groups.Length == 0)
        {
            _output.WriteLine("Skipped: No groups available");
            return;
        }

        var avatar = await _svc.GetGroupAvatar(_config.PhoneNumber, groups[0].Id, TestContext.Current.CancellationToken);
        _output.WriteLine(avatar is not null
            ? $"GroupAvatar size={avatar.Length} bytes"
            : "GroupAvatar returned null (no avatar set)");
    }

    #endregion

    #region Identities

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Identities)]
    public async Task ListIdentities_ReturnsIdentities()
    {
        var result = await _svc.ListIdentities(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Identities={result.Length}");
    }

    [Fact(Skip = "TrustIdentity requires an untrusted identity to trust")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Identities)]
    public async Task TrustIdentity_RequiresUntrustedIdentity()
    {
        var result = await _svc.TrustIdentity(_config.PhoneNumber, "+10000000001", trustAllKnownKeys: true,
            cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"TrustIdentity={result}");
    }

    #endregion

    #region Attachments

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Attachments)]
    public async Task ListAttachments_ReturnsAttachments()
    {
        var result = await _svc.ListAttachments(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"Attachments={result.Length}");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Attachments)]
    public async Task GetAttachment_ReturnsNullForMissingId()
    {
        var result = await _svc.GetAttachment("nonexistent-attachment-id", TestContext.Current.CancellationToken);
        Assert.Null(result);
        _output.WriteLine("GetAttachment returned null for nonexistent id (expected)");
    }

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Attachments)]
    public async Task DeleteAttachment_ReturnsFalseForMissingId()
    {
        var result = await _svc.DeleteAttachment("nonexistent-attachment-id", TestContext.Current.CancellationToken);
        Assert.False(result);
        _output.WriteLine($"DeleteAttachment for nonexistent id={result}");
    }

    #endregion

    #region Profile

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Profile)]
    public async Task UpdateProfile_ReturnsTrue()
    {
        var profile = new UpdateProfileRequest
        {
            Name = "Test Profile",
            About = "Integration test"
        };
        var result = await _svc.UpdateProfile(_config.PhoneNumber, profile, TestContext.Current.CancellationToken);
        Assert.True(result);
        _output.WriteLine($"UpdateProfile={result}");
    }

    #endregion

    #region Search

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.Search)]
    public async Task SearchNumbers_ReturnsResults()
    {
        var result = await _svc.SearchNumbers(_config.PhoneNumber, [_config.PhoneNumber], TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"SearchResults={result.Length}");
        _output.WriteLine($"RegisteredResults={result.Count(r => r.Registered)}");
    }

    #endregion

    #region Sticker Packs

    [Fact]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.StickerPacks)]
    public async Task ListStickerPacks_ReturnsPacks()
    {
        var result = await _svc.ListStickerPacks(_config.PhoneNumber, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        _output.WriteLine($"StickerPacks={result.Length}");
    }

    [Fact(Skip = "AddStickerPack requires a valid pack ID and key")]
    [Trait(SignalCliTraits.Feature, SignalCliTraits.StickerPacks)]
    public async Task AddStickerPack_RequiresPackInfo()
    {
        var result = await _svc.AddStickerPack(_config.PhoneNumber, "pack-id", "pack-key", TestContext.Current.CancellationToken);
        _output.WriteLine($"AddStickerPack={result}");
    }

    #endregion
}
