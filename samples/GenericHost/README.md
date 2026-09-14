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

## Voice Speech-to-Text Acceptance Harness

The sample can additionally prove the complete Signal voice-note path: receive, retrieve, convert, transcribe, compare a
transcript hash, and delete the attachment. It exists to validate the contract locally before that contract is
implemented in a consuming application, so it is deliberately manual and disabled by default.

The harness never sends a Signal reply.

### Prerequisites

* A registered or linked signal-cli account, as described above
* A speech-to-text server that accepts multipart `POST {Endpoint}{RequestPath}` with `file`, `language` and
  `response_format=json` parts and returns a JSON `text` property; stock
  [whisper.cpp](https://github.com/ggml-org/whisper.cpp/blob/master/examples/server/README.md) serves this at
  `/inference`. Run it on a port other than the signal-cli `8080`, for example `8081`.
* `ffmpeg` on `PATH`, or an absolute path in `VoiceSttHarness:FfmpegPath`, when `TranscodeToWav` is enabled. A stock
  whisper.cpp server accepts WAV only unless it was started with `--convert`, and Android voice notes are AAC.
* A device running Signal that can send a voice note to the registered account

### Privacy Behaviour

Audio stays in memory and is piped to ffmpeg through standard input and output, so nothing is written to disk. The
harness never logs, writes or echoes phone numbers, sender or recipient identity, group information, attachment
filenames, message text, transcript text, or audio bytes. Never store a spoken phrase, a recording, or a transcript
anywhere beneath this repository.

### Harness Configuration

Every key lives in the top-level `VoiceSttHarness` section. Tracked `appsettings.json` values are synthetic and the
harness ships disabled.

| Key | Default | Purpose |
| --- | --- | --- |
| `Enabled` | `false` | Turns the harness on; when `false` the worker only logs envelope arrival |
| `Endpoint` | `http://localhost:8081` | Base address of the speech-to-text server |
| `RequestPath` | `/inference` | Path appended to `Endpoint` |
| `Language` | `en` | Language hint sent as the `language` part |
| `MaxAttachmentBytes` | `26214400` | Largest declared attachment size that will be downloaded |
| `TimeoutMs` | `120000` | Per-request timeout for the transcription call |
| `FfmpegPath` | `ffmpeg` | ffmpeg executable name or absolute path |
| `TranscodeToWav` | `true` | Converts the audio to 16 kHz mono signed 16-bit PCM WAV before transcription |
| `DeleteAttachmentAfterProcessing` | `true` | Deletes the attachment from the signal-cli wrapper afterwards |
| `StopAfterFirstAccepted` | `false` | Stops the host after one accepted voice note, for repeatable acceptance runs |
| `ExpectedTranscriptSha256` | `null` | SHA-256 of the expected normalized transcript, never the phrase itself |

Supply local values through User Secrets so nothing sensitive is committed:

```powershell
dotnet user-secrets --project samples/GenericHost set "VoiceSttHarness:Enabled" "true"
dotnet user-secrets --project samples/GenericHost set "VoiceSttHarness:Endpoint" "http://localhost:8081"
dotnet user-secrets --project samples/GenericHost set "VoiceSttHarness:StopAfterFirstAccepted" "true"
dotnet user-secrets --project samples/GenericHost set "VoiceSttHarness:ExpectedTranscriptSha256" "<locally-computed-sha256>"
```

Compute the expected hash locally from the phrase you intend to speak, after applying the same normalization the
harness uses — trim, collapse internal whitespace runs to a single space, then lowercase with the invariant culture:

```powershell
$normalized = 'the agreed synthetic phrase'
[BitConverter]::ToString(
    [System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normalized))
).Replace('-', '').ToLowerInvariant()
```

### Manual Acceptance Procedure

1. Start the signal-cli container and the speech-to-text server, then confirm the server transcribes a locally
   generated synthetic WAV before involving Signal.
2. Configure the User Secrets above and start the sample with `dotnet run --project samples/GenericHost`.
3. Wait for the `voice acceptance harness enabled=True` line.
4. From the Signal app, send exactly one voice note to the registered account.
5. Read the safe evidence fields below from the console output.
6. Confirm the attachment is gone by requesting it again through the wrapper; it should return `404`.

### Safe Evidence Fields

The harness emits only non-content evidence:

| Field | Meaning |
| --- | --- |
| `contentType` | Declared MIME type of the selected attachment |
| `hasFilename` | Whether a filename was present; the filename itself is never logged |
| `declaredBytes` / `downloadedBytes` | Size claimed by the sender and size actually retrieved |
| `sha256` | SHA-256 of the downloaded audio |
| `sourceBytes` / `convertedBytes` | Byte counts either side of the optional ffmpeg conversion |
| `elapsedMs` | Transcription round-trip duration |
| `transcriptChars` / `transcriptSha256` | Length and hash of the normalized transcript, never its text |
| `expectedTranscriptMatched` | Whether the transcript hash matched `ExpectedTranscriptSha256` |
| `deleted` | Whether the explicit attachment deletion succeeded |

The run passes when the byte counts are coherent, conversion succeeds where enabled, a non-empty transcript is
returned, `expectedTranscriptMatched=True`, and deletion succeeds.

## Configuration

The sample reads `CasCap:SignalCliConfig` from `appsettings.json`, .NET User Secrets, and environment variables. See the
[library configuration reference](../../src/CasCap.Api.SignalCli/README.md#configuration) for every option. The
sample-only `VoiceSttHarness` section is documented above.

## Dependencies

### NuGet packages

| Package | Purpose |
| --- | --- |
| `Microsoft.Extensions.Hosting` | Configuration, dependency injection, logging, and worker lifetime |
| `Microsoft.Extensions.Http` | `IHttpClientFactory` registration for the speech-to-text client |

### Project references

| Project | Purpose |
| --- | --- |
| `CasCap.Api.SignalCli` | Typed REST and WebSocket Signal client |

### External executables

| Executable | Purpose |
| --- | --- |
| `ffmpeg` | Converts received audio to 16 kHz mono signed 16-bit PCM WAV when `TranscodeToWav` is enabled |
