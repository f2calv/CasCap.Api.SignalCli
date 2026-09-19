# CasCap.Api.SignalCli

CasCap.Api.SignalCli provides typed .NET 10 clients for the signal-cli REST API, including HTTP polling and persistent
JSON-RPC WebSocket message reception.

[CasCap.Api.SignalCli-badge]: https://img.shields.io/nuget/v/CasCap.Api.SignalCli?color=blue
[CasCap.Api.SignalCli-url]: https://nuget.org/packages/CasCap.Api.SignalCli

![CI](https://github.com/f2calv/CasCap.Api.SignalCli/actions/workflows/ci.yml/badge.svg)
[![NuGet][CasCap.Api.SignalCli-badge]][CasCap.Api.SignalCli-url]

## Packages

| Package | Purpose |
| --- | --- |
| [`CasCap.Api.SignalCli`](src/CasCap.Api.SignalCli) | Typed REST client, receive transports, health check, and DI registration |
| [`CasCap.Api.SignalCli.AspNetCore`](src/CasCap.Api.SignalCli.AspNetCore) | Optional authenticated, versioned MVC query controller |

## Quick Start

```powershell
dotnet package add CasCap.Api.SignalCli
```

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSignalCli(builder.Configuration);

await builder.Build().RunAsync();
```

Configure the REST API endpoint, transport mode, and registered account under `CasCap:SignalCliConfig`. Store real phone
numbers and Basic Auth credentials in .NET User Secrets, environment variables, or another secret-backed provider.

```json
{
  "CasCap": {
    "SignalCliConfig": {
      "TransportMode": "JsonRpc",
      "BaseAddress": "http://localhost:8080",
      "PhoneNumber": "+10000000000"
    }
  }
}
```

See the [core package README](src/CasCap.Api.SignalCli/README.md) for the complete API and configuration reference.

## Local Sample

The [Generic Host sample](samples/GenericHost) runs against the pinned signal-cli REST API in
[`docker-compose.yml`](docker-compose.yml). It verifies the API and consumes one long-lived receive stream without logging
sender identities or message content. It also carries the voice speech-to-text acceptance harness.

## Container Image

The library ships as a NuGet package; the image exists for the sample, and its reason to exist is ffmpeg. The voice
harness converts audio before transcribing it, and baking ffmpeg into the image removes "install ffmpeg on the host"
from the acceptance procedure.

[`Dockerfile`](Dockerfile) cross-compiles a single multi-architecture image for `linux/amd64` and `linux/arm64`.
[`build.ps1`](build.ps1) and [`build.sh`](build.sh) mirror the `containerize` job in
[`ci.yml`](.github/workflows/ci.yml) and derive every provenance value from git.

```powershell
./build.ps1                     # local multi-arch validation build
./build.ps1 -Push               # publish to ghcr.io, tagged from GitVersion
docker compose --profile harness up --build harness
```

## Tests

Credential-free unit tests run in CI. Integration tests target the local container and require a registered Signal
account; configuration is supplied through the test project's User Secrets or environment variables.

```powershell
dotnet test src/CasCap.Api.SignalCli.Tests/CasCap.Api.SignalCli.Tests.csproj --filter-not-trait Category=Integration
```

## Resources

* [signal-cli REST API](https://github.com/bbernhard/signal-cli-rest-api)
* [Signal](https://signal.org/)
* [Issue tracker](https://github.com/f2calv/CasCap.Api.SignalCli/issues)

## License

This project is released under [The Unlicense](LICENSE). See the [LICENSE](LICENSE) file for details.
