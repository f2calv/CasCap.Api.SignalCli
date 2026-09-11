# CasCap.Api.SignalCli Generic Host Sample

This .NET 10 worker verifies a local signal-cli REST API and continuously consumes its receive stream.

## Run Locally

Start the pinned signal-cli REST API container from the repository root:

```powershell
docker compose up --detach
```

Register an account or link the container as a Signal device by following the
[signal-cli REST API account guide](https://github.com/bbernhard/signal-cli-rest-api/blob/master/doc/REGISTER_ACCOUNT.md).
The named Docker volume preserves that account across container restarts.

Set the registered account number without committing it:

```powershell
dotnet user-secrets --project samples/GenericHost set "CasCap:SignalCliConfig:PhoneNumber" "+10000000000"
dotnet run --project samples/GenericHost
```

The worker reports API version information and whether each received envelope contains a data message. It does not log
sender identities or message content.

## Configuration

The sample reads `CasCap:SignalCliConfig` from `appsettings.json`, .NET User Secrets, and environment variables. See the
[library configuration reference](../../src/CasCap.Api.SignalCli/README.md#configuration) for every option.

## Dependencies

### NuGet packages

| Package | Purpose |
| --- | --- |
| `Microsoft.Extensions.Hosting` | Configuration, dependency injection, logging, and worker lifetime |

### Project references

| Project | Purpose |
| --- | --- |
| `CasCap.Api.SignalCli` | Typed REST and WebSocket Signal client |
