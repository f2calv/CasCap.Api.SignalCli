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

### Modes

| Mode | Trigger | What it proves |
| --- | --- | --- |
| File | `VoiceSttHarness:AudioFilePath` is set | ffmpeg conversion and the speech-to-text contract, against a local recording. signal-cli is never contacted, so no second Signal registration is needed. The host processes the file once and exits. |
| Signal | `VoiceSttHarness:AudioFilePath` is unset and `Enabled` is `true` | The complete path, including attachment retrieval and deletion. Needs a registered or linked account. |

Start with file mode. It removes every Signal variable from the first run, and a failure there is unambiguously an
ffmpeg or speech-to-text problem.

> Registering the same phone number with a second signal-cli instance breaks the first one. Point
> `CasCap:SignalCliConfig:BaseAddress` at an existing instance instead.

### Group Scoping

Every consumer of one signal-cli account sees the **whole** receive stream, so several deployments can share an
instance only by filtering to their own Signal group. Signal mode therefore requires `VoiceSttHarness:GroupName`
(or `GroupId`), and ignores every envelope from any other group.

Give the harness its own dedicated group. Pointing it at a group another deployment already services would make two
consumers act on the same message.

### Providers

`VoiceSttHarness:Provider` selects the wire contract. Both return a top-level JSON `text` property.

| Provider | Server | Request |
| --- | --- | --- |
| `WhisperAsr` (default) | [openai-whisper-asr-webservice](https://github.com/ahmetoner/whisper-asr-webservice) | multipart `POST /asr?task=transcribe&language=…&encode=…&output=json`, part `audio_file` |
| `WhisperCpp` | [whisper.cpp](https://github.com/ggml-org/whisper.cpp/blob/master/examples/server/README.md) server | multipart `POST /inference`, parts `file`, `language`, `response_format=json` |

When `TranscodeToWav` is enabled the harness has already produced 16 kHz mono PCM WAV, so the `WhisperAsr` request sets
`encode=false` and skips the server-side ffmpeg pass.

`RequestPath` overrides the provider's default route; leave it unset to use `/asr` or `/inference` respectively.

### Prerequisites

* A speech-to-text server reachable from wherever the sample runs
* `ffmpeg` on `PATH`, or an absolute path in `VoiceSttHarness:FfmpegPath`, when `TranscodeToWav` is enabled. The
  repository's own image installs it, so running the harness in a container removes this prerequisite entirely.
* A local audio recording for file mode, or a registered/linked signal-cli account and a device that can send a voice
  note for Signal mode

### Privacy Behaviour

Audio stays in memory and is piped to ffmpeg through standard input and output, so nothing is written to disk. The
harness never logs, writes or echoes phone numbers, sender or recipient identity, group information, attachment
filenames, message text, transcript text, or audio bytes. The configured `AudioFilePath` is not logged either, because a
local fixture is commonly named after the phrase it contains. Never commit a spoken phrase, a recording, or a
transcript; `testdata/`, `.secrets/` and `secrets.json` are gitignored for exactly that reason.

### Harness Configuration

Every key lives in the top-level `VoiceSttHarness` section. Tracked `appsettings.json` values are synthetic and the
harness ships disabled.

| Key | Default | Purpose |
| --- | --- | --- |
| `Enabled` | `false` | Turns Signal mode on; when `false` the worker only logs envelope arrival |
| `Provider` | `WhisperAsr` | Selects the speech-to-text wire contract |
| `AudioFilePath` | `null` | Switches to file mode and transcribes this recording once, without contacting signal-cli |
| `GroupName` | `null` | The Signal group the harness listens to, resolved by name. Required in Signal mode |
| `GroupId` | `null` | Raw group identifier, used when `GroupName` cannot be resolved |
| `Endpoint` | `http://localhost:8081` | Base address of the speech-to-text server |
| `RequestPath` | `null` | Overrides the provider's default route (`/asr` or `/inference`) |
| `Language` | `en` | Language hint sent with the request |
| `MaxAttachmentBytes` | `26214400` | Largest audio payload that will be processed |
| `TimeoutMs` | `120000` | Per-request timeout for the transcription call |
| `FfmpegPath` | `ffmpeg` | ffmpeg executable name or absolute path |
| `TranscodeToWav` | `true` | Converts the audio to 16 kHz mono signed 16-bit PCM WAV before transcription |
| `DeleteAttachmentAfterProcessing` | `true` | Deletes the attachment from the signal-cli wrapper afterwards |
| `StopAfterFirstAccepted` | `false` | Stops the host after one accepted voice note, for repeatable acceptance runs |
| `ExpectedTranscriptSha256` | `null` | SHA-256 of the expected normalized transcript, never the phrase itself |

Supply local values through User Secrets so nothing sensitive is committed:

```powershell
dotnet user-secrets --project samples/GenericHost set "VoiceSttHarness:Endpoint" "http://localhost:8081"
dotnet user-secrets --project samples/GenericHost set "VoiceSttHarness:AudioFilePath" "testdata/sample.aac"
dotnet user-secrets --project samples/GenericHost set "VoiceSttHarness:ExpectedTranscriptSha256" "<locally-computed-sha256>"
```

The sample calls `AddUserSecrets` unconditionally rather than relying on the Development-only default, so the same
`secrets.json` is read when it runs from a container under a different environment name.

Compute the expected hash locally from the phrase you intend to speak, after applying the same normalization the
harness uses — trim, collapse internal whitespace runs to a single space, then lowercase with the invariant culture:

```powershell
$normalized = 'the agreed synthetic phrase'
[BitConverter]::ToString(
    [System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normalized))
).Replace('-', '').ToLowerInvariant()
```

### Manual Acceptance Procedure

1. Start the speech-to-text endpoint and confirm it transcribes a locally generated synthetic WAV.
2. Configure the User Secrets above, including `AudioFilePath`, and run the sample.
3. Read the safe evidence fields below from the console output and confirm `expectedTranscriptMatched=True`.
4. Clear `AudioFilePath`, set `Enabled` to `true`, restart, and send one voice note from the Signal app.
5. Confirm the attachment is gone by requesting it again through the wrapper; it should return `404`.

On the first run leave `ExpectedTranscriptSha256` unset, because the harness never prints the transcript. Hash the
phrase you expect, compare it against the reported `transcriptSha256`, then configure it to make the match a gate.

### Running in a Container

The repository image carries ffmpeg, and the `harness` compose profile also starts a local whisper-asr sidecar, so the
whole path runs without installing anything on the host:

```powershell
docker compose --profile harness up --detach whisper-asr   # first start downloads the model
docker compose --profile harness run --rm harness
```

The sidecar runs the same image as the deployed cluster service, so the wire contract proven locally is the one that
runs in production. It defaults to the `base` model; `small` and `medium` are more accurate and much slower on CPU.

The compose service mounts `testdata/` read-only at `/testdata` and a secrets directory at the container's user-secrets
path, so `AudioFilePath` should be set to `/testdata/<file>`. By default the secrets directory is `./.secrets`; set
`USER_SECRETS_DIR` to share the real store instead:

| OS | `USER_SECRETS_DIR` |
| --- | --- |
| Windows | `$env:APPDATA/Microsoft/UserSecrets/890bd946-ed0b-4c13-abdf-070bc0b3fabc` |
| Linux, macOS | `$HOME/.microsoft/usersecrets/890bd946-ed0b-4c13-abdf-070bc0b3fabc` |

That identifier is declared once in [`Directory.Build.props`](../../Directory.Build.props) and shared with the test
project, so a signal-cli account or speech-to-text endpoint is configured once and serves both.

Environment variables still take precedence over user secrets, so a `secrets.json` pointing at a cluster endpoint
resolves to the compose sidecar when run through compose.

See the [root README](../../README.md#container-image) for the multi-architecture build scripts.

### Safe Evidence Fields

The harness emits only non-content evidence:

| Field | Meaning |
| --- | --- |
| `provider` | The selected speech-to-text wire contract |
| `contentType` | Declared MIME type of the selected attachment, or the type inferred from the file extension |
| `hasFilename` | Whether a filename was present; the filename itself is never logged |
| `declaredBytes` / `downloadedBytes` | Size claimed by the sender and size actually retrieved |
| `sha256` | SHA-256 of the source audio |
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
| `Microsoft.Extensions.Configuration.UserSecrets` | Reads `secrets.json` outside the Development environment |
| `Microsoft.Extensions.Http` | `IHttpClientFactory` registration for the speech-to-text client |

### Project references

| Project | Purpose |
| --- | --- |
| `CasCap.Api.SignalCli` | Typed REST and WebSocket Signal client |

### External executables

| Executable | Purpose |
| --- | --- |
| `ffmpeg` | Converts received audio to 16 kHz mono signed 16-bit PCM WAV when `TranscodeToWav` is enabled |
