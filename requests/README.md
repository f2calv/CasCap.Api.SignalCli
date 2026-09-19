# REST Client Requests

These request collections exercise the Signal CLI client and ASP.NET Core controller through the VS
Code REST Client extension.

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

Sending or receiving messages has external side effects. Review each request before executing it.
