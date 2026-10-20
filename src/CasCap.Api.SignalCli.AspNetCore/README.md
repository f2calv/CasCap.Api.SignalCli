# CasCap.Api.SignalCli.AspNetCore

Authenticated, read-only Signal query endpoints for .NET 10 ASP.NET Core applications. The package
exposes `SignalCliController` over [CasCap.Api.SignalCli](https://www.nuget.org/packages/CasCap.Api.SignalCli)
without adding MVC dependencies to worker applications.

## Installation

```bash
dotnet package add CasCap.Api.SignalCli.AspNetCore
```

## Controller

`SignalCliController` is a thin pass-through over
[`ISignalCliClient`](https://github.com/f2calv/CasCap.Api.SignalCli/blob/main/src/CasCap.Api.SignalCli/Abstractions/ISignalCliClient.cs).
Every action flows the request abort token into the underlying call, and maps a `null` result to
`404 Not Found` rather than `200 OK` with a null body.

| Method | Route | Description |
| --- | --- | --- |
| `GetAbout` | `GET /api/v1/signalcli/about` | Returns signal-cli version and build info |
| `GetConfiguration` | `GET /api/v1/signalcli/configuration` | Retrieves the signal-cli configuration |
| `ListAccounts` | `GET /api/v1/signalcli/accounts` | Lists all registered accounts |
| `ListContacts` | `GET /api/v1/signalcli/contacts?number=` | Lists contacts for an account |
| `ListGroups` | `GET /api/v1/signalcli/groups?number=` | Lists groups for an account |
| `ListLinkedDevices` | `GET /api/v1/signalcli/devices?number=` | Lists linked devices for an account |
| `ListIdentities` | `GET /api/v1/signalcli/identities?number=` | Lists known identities for an account |
| `ListAttachments` | `GET /api/v1/signalcli/attachments` | Lists all stored attachment identifiers |
| `ListStickerPacks` | `GET /api/v1/signalcli/sticker-packs?number=` | Lists installed sticker packs for an account |

The controller carries `[Authorize]`. Configure an authentication scheme in the host; the endpoints expose account metadata, contacts and group membership and must not be left anonymous.

## Registration

In an ASP.NET Core host with its authentication scheme and authorization services already configured,
register the controller assembly, MVC API versioning and the Signal client:

```csharp
using Asp.Versioning;
using CasCap.Controllers;
using CasCap.Extensions;

builder.Services.AddSignalCli(builder.Configuration);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(SignalCliController).Assembly);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
}).AddMvc();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();
```

`builder` is the host's `WebApplicationBuilder`. `AddSignalCli` supplies `ISignalCliClient`, the
controller's only constructor dependency. It requires the same account and endpoint settings as the
[core package configuration](https://github.com/f2calv/CasCap.Api.SignalCli/blob/main/src/CasCap.Api.SignalCli/README.md#configuration).

The controller does not select an authentication scheme for you. Do not remove `[Authorize]` to work
around missing host authentication.

## License

Released under the [MIT License](https://github.com/f2calv/CasCap.Api.SignalCli/blob/main/LICENSE).
