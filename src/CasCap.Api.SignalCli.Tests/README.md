# CasCap.Api.SignalCli.Tests

Integration and unit tests for the Signal messenger library ([CasCap.Api.SignalCli](../CasCap.Api.SignalCli)). The unit tests run against a stubbed `HttpMessageHandler` and need no signal-cli server or credentials. The integration tests exercise `SignalCliRestClientService` against a running [signal-cli REST API](https://bbernhard.github.io/signal-cli-rest-api/) instance.

## Tests

| Class | Folder | Methods | Test Cases |
| --- | --- | --- | --- |
| `SignalCliRestClientServiceUnitTests` | Unit | 15 | 17 |
| `SignalCliConnectionHealthCheckUnitTests` | Unit | 1 | 1 |
| `SignalCliRegistrationUnitTests` | Unit | 9 | 15 |
| `SignalCliJsonRpcClientServiceUnitTests` | Unit | 10 | 13 |
| `SignalCliRestClientServiceTests` | Integration | 51 | 51 |
| `SignalCliJsonRpcClientServiceTests` | Integration | 4 | 4 |
| **Total** | | **90** | **101** |

The 46 unit test cases are the credential-free subset CI runs.

## Trait Categories

| Category | Description |
| --- | --- |
| `Unit` | Self-contained tests using a stubbed `HttpMessageHandler`; no server or credentials |
| `Integration` | Tests requiring a running signal-cli REST API instance |
| `WebSocket` | Self-contained unit tests for WebSocket/JSON-RPC logic |

## Skipped Tests

| Skip Reason | Count |
| --- | --- |
| Requires signal-cli REST API running in json-rpc mode | 3 |
| Requires a dedicated test phone number | 2 |
| Destructive or cannot be undone | 4 |
| Requires a second phone number | 2 |
| Requires a real challenge token / device-link URI / invite link / pack ID | 4 |
| Requires an untrusted identity to trust | 1 |
| VerifyNumber requires dedicated test phone number and token | 1 |
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
    │   ├── SignalCliConnectionHealthCheckUnitTests.cs
    │   ├── SignalCliJsonRpcClientServiceUnitTests.cs
    │   ├── SignalCliRegistrationUnitTests.cs
    │   ├── SignalCliRestClientServiceUnitTests.cs
    │   └── StubHttpMessageHandler.cs
    └── Integration/
        ├── TestBase.cs
        ├── SignalCliRestClientServiceTests.cs
        └── SignalCliJsonRpcClientServiceTests.cs
```

## Prerequisites

The unit tests have no prerequisites. The integration tests require:

* A running signal-cli REST API accessible from the test host
* A registered test account
* `CasCap:SignalCliConfig:PhoneNumber` supplied through User Secrets or an environment variable
* Optional `SignalCliTests:ExistingGroupName` for tests that send to an existing group

Start the repository's pinned container and configure the test account without committing it:

```powershell
docker compose up --detach
dotnet user-secrets --project src/CasCap.Api.SignalCli.Tests set "CasCap:SignalCliConfig:PhoneNumber" "+10000000000"
dotnet user-secrets --project src/CasCap.Api.SignalCli.Tests set "SignalCliTests:ExistingGroupName" "SignalCli integration"
```

## Running the Tests

The credential-free subset, matching CI:

```bash
dotnet test src/CasCap.Api.SignalCli.Tests/CasCap.Api.SignalCli.Tests.csproj --filter-not-trait Category=Integration
```

Everything, including the tests that need a live server:

```bash
dotnet test src/CasCap.Api.SignalCli.Tests/CasCap.Api.SignalCli.Tests.csproj
```

## Dependencies

### Project references

| Project | Purpose |
| --- | --- |
| `CasCap.Api.SignalCli` | Library under test |
| `CasCap.Common.Configuration` | Configuration binding |
| `CasCap.Common.Extensions` | Shared extension helpers |
| `CasCap.Common.Logging` | xUnit logging integration |
| `CasCap.Common.Net` | HTTP helpers |
| `CasCap.Common.Testing` | `AddXUnitLogging` and test utilities |

## License

This project is released under [The Unlicense](../../LICENSE). See the [LICENSE](../../LICENSE) file for details.
