# CasCap.Api.SignalCli.Tests

Credential-free transport, registration and diagnostics tests for
[CasCap.Api.SignalCli](../CasCap.Api.SignalCli/README.md), plus integration cases requiring a dedicated
signal-cli REST API account. All tests target .NET 10.

## Tests

| Class | Folder | Methods | Test Cases |
| --- | --- | --- | --- |
| `SignalCliRestClientServiceUnitTests` | Unit | 16 | 18 |
| `SignalCliConnectionHealthCheckUnitTests` | Unit | 1 | 1 |
| `SignalGroupUnitTests` | Unit | 1 | 5 |
| `SignalCliRegistrationUnitTests` | Unit | 12 | 18 |
| `SignalCliJsonRpcClientServiceUnitTests` | Unit | 10 | 13 |
| `SignalCliTelemetryUnitTests` | Unit | 1 | 1 |
| `SignalCliSendLoggingTests` | Unit | 1 | 3 |
| `SignalCliRestClientServiceTests` | Integration | 51 | 51 |
| `SignalCliJsonRpcClientServiceTests` | Integration | 4 | 4 |
| **Total** | | **97** | **114** |

These are source-declared counts, not execution results. The credential-free subset contains
59 expanded cases; the 55 declared integration cases include the 17 static skips below.

## Trait Categories

| Category | Description |
| --- | --- |
| `Unit` | Self-contained tests using a stubbed `HttpMessageHandler`; no server or credentials |
| `Integration` | Tests requiring a running signal-cli REST API instance |
| `WebSocket` | Self-contained unit tests for WebSocket/JSON-RPC logic |

Integration tests additionally use `Feature` values `General`, `Messaging`, `Registration`, `Accounts`,
`Contacts`, `Devices`, `Groups`, `Identities`, `Attachments`, `Profile`, `Search` and `StickerPacks`.
`Transport=Polling` marks the HTTP receive case that is incompatible with JSON-RPC server mode.
The send-logging cases have no trait; excluding `Category=Integration` includes them.

## Skipped Tests

| Skip Reason | Count |
| --- | --- |
| Requires signal-cli REST API running in json-rpc mode | 3 |
| Requires a dedicated registration number, or a number and verification token | 2 |
| Destructive or cannot be undone | 4 |
| Requires a linked device to remove | 1 |
| Requires a second phone number | 2 |
| Requires a real challenge token / device-link URI / invite link / pack ID | 4 |
| Requires an untrusted identity to trust | 1 |
| **Total** | **17** |

## File Structure

```text
CasCap.Api.SignalCli.Tests/
├── CasCap.Api.SignalCli.Tests.csproj
├── GlobalUsings.cs
├── README.md
├── appsettings.Test.json
├── xunit.runner.json
└── Tests/
    ├── Unit/
    │   ├── RecordedCall.cs
    │   ├── SignalCliConnectionHealthCheckUnitTests.cs
    │   ├── SignalGroupUnitTests.cs
    │   ├── SignalCliJsonRpcClientServiceUnitTests.cs
    │   ├── SignalCliRegistrationUnitTests.cs
    │   ├── SignalCliRestClientServiceUnitTests.cs
    │   ├── SignalCliSendLoggingTests.cs
    │   ├── SignalCliTelemetryUnitTests.cs
    │   ├── StubHttpClientFactory.cs
    │   └── StubHttpMessageHandler.cs
    └── Integration/
        ├── TestBase.cs
        ├── SignalCliTraits.cs
        ├── SignalCliRestClientServiceTests.cs
        └── SignalCliJsonRpcClientServiceTests.cs
```

## Prerequisites

Use the .NET 10 SDK and run commands from the repository root. Unit tests need no external service.
Integration tests require:

* A running signal-cli REST API accessible from the test host
* A registered test account
* `CasCap:SignalCliConfig:PhoneNumber` supplied through User Secrets or an environment variable
* Optional `SignalCliTests:ExistingGroupName` for tests that send to an existing group

Start the repository's pinned container and configure the test account without committing it:

```powershell
docker compose up --detach --wait signalcli
dotnet user-secrets --project src/CasCap.Api.SignalCli.Tests set "CasCap:SignalCliConfig:PhoneNumber" "+10000000000"
dotnet user-secrets --project src/CasCap.Api.SignalCli.Tests set "SignalCliTests:ExistingGroupName" "SignalCli integration"
```

## Running the Tests

The credential-free subset, matching CI:

```bash
dotnet test --project src/CasCap.Api.SignalCli.Tests/CasCap.Api.SignalCli.Tests.csproj --filter-not-trait Category=Integration
```

Against the repository's JSON-RPC Compose service, additionally exclude the HTTP-polling-only case:

```bash
dotnet test --project src/CasCap.Api.SignalCli.Tests/CasCap.Api.SignalCli.Tests.csproj --filter-not-trait Transport=Polling
```

This command includes live integration cases. They can send messages and mutate account, contact,
group or attachment state; use only a disposable test account and review the cases first.
Configuration loads `appsettings.Test.json`, optional `appsettings.Local.Test.json`, User Secrets,
then environment variables. A missing account or service is not an automatic skip for active
integration cases. The sample and tests share their User Secrets store.

## License

Released under the [MIT License](../../LICENSE).
