# REST Client Requests

Manual HTTP collections for the signal-cli REST API and its optional ASP.NET Core query controller,
using the VS Code REST Client extension.

## Setup

1. Copy `requests/.env.example` to `requests/.env`.
2. Set local endpoints, authentication values, and the Signal phone number.
3. Open a `.http` file and select **Send Request** above the request to execute.

`requests/.env` is ignored by Git. Keep credentials, phone numbers, and private endpoints there.

## Collections

| File | Purpose |
| --- | --- |
| `signalcli-client.http` | Direct signal-cli REST API operations |
| `signalcli-controller.http` | ASP.NET Core controller operations |

`SIGNAL_CLI_BASE_URL`, `SIGNAL_CLI_AUTH` and `SIGNAL_CLI_PHONE_NUMBER` target the upstream API.
The controller collection uses `HAUS_BASE_URL` and `HAUS_API_AUTH` to target your own ASP.NET Core host;
those variable names do not require a particular consuming application. Authentication variables
hold the credential portion of a Basic authorization header, not an entire header. Treat them as secrets.

Sending or receiving messages has external side effects. Review each request before executing it.
