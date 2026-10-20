# CasCap.Api.SignalCli

Typed .NET 10 clients for sending and receiving Signal messages through the
[signal-cli REST API](https://github.com/bbernhard/signal-cli-rest-api). Choose HTTP polling or a
persistent JSON-RPC WebSocket receive stream, with optional ASP.NET Core query endpoints.

[CasCap.Api.SignalCli-badge]: https://img.shields.io/nuget/v/CasCap.Api.SignalCli?color=blue
[CasCap.Api.SignalCli-url]: https://nuget.org/packages/CasCap.Api.SignalCli

[![CI status](https://github.com/f2calv/CasCap.Api.SignalCli/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/f2calv/CasCap.Api.SignalCli/actions/workflows/ci.yml)
[![SonarQube Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=f2calv_CasCap.Api.SignalCli&metric=alert_status)](https://sonarcloud.io/dashboard?id=f2calv_CasCap.Api.SignalCli)

## Packages

| Package | Purpose | NuGet |
| --- | --- | --- |
| [CasCap.Api.SignalCli](src/CasCap.Api.SignalCli/README.md) | REST client, receive transports, health check and DI registration | [![NuGet version][CasCap.Api.SignalCli-badge]][CasCap.Api.SignalCli-url] |
| [CasCap.Api.SignalCli.AspNetCore](src/CasCap.Api.SignalCli.AspNetCore/README.md) | Authenticated, versioned MVC query controller | [![NuGet version](https://img.shields.io/nuget/v/CasCap.Api.SignalCli.AspNetCore)](https://www.nuget.org/packages/CasCap.Api.SignalCli.AspNetCore) |

## Quick Start

```powershell
dotnet package add CasCap.Api.SignalCli
```

Use a .NET 10 Generic Host application and a reachable signal-cli REST API with a registered or linked
account. Configure `CasCap:SignalCliConfig` in the host's configuration:

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

The number is a placeholder; supply your registered account through .NET User Secrets, environment
variables or another secret-backed provider. `TransportMode` must match the server's `MODE`.

```csharp
using CasCap.Abstractions;
using CasCap.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSignalCli(builder.Configuration);

using var host = builder.Build();
var client = host.Services.GetRequiredService<ISignalCliClient>();
var about = await client.GetAbout();
Console.WriteLine(about is null ? "API unavailable" : "API reachable");
```

Registration alone does not start message reception. Enumerate
[`ISignalCliReceiver.StreamMessagesAsync`](src/CasCap.Api.SignalCli/Abstractions/ISignalCliReceiver.cs)
from a hosted worker; the [core package README](src/CasCap.Api.SignalCli/README.md) explains the receive
contract, configuration and failure handling.

The core package also emits an OpenTelemetry-compatible `CasCap.Api.SignalCli` meter and activity
source. Exporter configuration remains the consuming host's responsibility.

## Local Sample

The [Generic Host sample](samples/GenericHost/README.md) runs against the pinned signal-cli REST API in
[`docker-compose.yml`](docker-compose.yml). It verifies the API and consumes one long-lived receive stream without logging
sender identities or message content. Attachment metadata and downloaded byte counts are reported without logging
filenames or attachment content.

## Development

With the .NET 10 SDK, run the credential-free subset:

```powershell
dotnet test --project src/CasCap.Api.SignalCli.Tests/CasCap.Api.SignalCli.Tests.csproj --filter-not-trait Category=Integration
```

Integration tests require a dedicated registered account and can send messages or change remote
state. See the [test README](src/CasCap.Api.SignalCli.Tests/README.md) for prerequisites, exclusions and
the source-declared inventory.

## Documentation

- [REST Client request collections](requests/README.md)
- [Signal](https://signal.org/)

## License

Released under the [MIT License](LICENSE).
