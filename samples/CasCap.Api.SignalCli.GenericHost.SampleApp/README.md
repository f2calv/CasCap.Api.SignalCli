# CasCap.Api.SignalCli.GenericHost.SampleApp

This .NET 11 worker verifies a local signal-cli REST API and continuously consumes its receive stream.

## Run Locally

Start the pinned signal-cli REST API container from the repository root:

```powershell
docker compose up --detach --wait signalcli
```

Register an account or link the container as a Signal device by following the
[signal-cli REST API setup guide](https://github.com/bbernhard/signal-cli-rest-api#getting-started).
The named Docker volume preserves that account across container restarts.

Stop other receivers for this account before running the sample: it consumes the live inbound
stream rather than observing an independent copy.

Set the registered account number without committing it:

```powershell
dotnet user-secrets --project .\samples\CasCap.Api.SignalCli.GenericHost.SampleApp set "CasCap:SignalCliConfig:PhoneNumber" "+10000000000"
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project .\samples\CasCap.Api.SignalCli.GenericHost.SampleApp
```

The worker reports API version information and the type of each received envelope. For data messages it reports whether
text is present and the number of attachments. Attachment metadata includes media type, declared size
and whether a filename or identifier is present; attachments with an id
are downloaded through the library so the sample also demonstrates the binary attachment API and reports only whether
the download succeeded and its byte count. It never logs sender identities, filenames, message content, or attachment
content, and it never deletes attachments or transcribes audio.

## Configuration

The Generic Host loads base/environment JSON, optional `appsettings.Local.json`,
`appsettings.Local.{Environment}.json` and `appsettings.Tests.Local.json`, Development User Secrets,
environment variables and command-line configuration, in ascending priority. Local files use the
host's content root, the same directory as its default JSON settings. They are gitignored and
excluded from publishing and packaging. User Secrets override duplicate local values.
The example sets Development explicitly; outside Development,
supply the account through another provider, such as `CasCap__SignalCliConfig__PhoneNumber`. See the
[library configuration reference](../../src/CasCap.Api.SignalCli/README.md#configuration) for every option.

Debug uses an adjacent [CasCap.Common checkout](https://github.com/f2calv/CasCap.Common) for the new
local-overlay helper. Release requires the next shared configuration package containing
`AddLocalConfiguration` and an updated package pin.

`TransportMode` defaults to `JsonRpc`, matching the repository's Compose service. The sample and test
project share a User Secrets store. Stop the container with `docker compose down`; do not add
`--volumes` unless you intend to remove the stored account.
