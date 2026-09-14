using System.Diagnostics;

namespace CasCap.Samples;

/// <summary>Converts audio to 16 kHz mono signed 16-bit PCM WAV using an external ffmpeg process.</summary>
/// <remarks>
/// Sample-only acceptance code. Audio is piped through ffmpeg standard input and standard output so it never
/// touches the file system. ffmpeg must be on <c>PATH</c> or configured through
/// <see cref="VoiceSttHarnessConfig.FfmpegPath"/>.
/// </remarks>
public sealed partial class FfmpegAudioTranscoder(
    ILogger<FfmpegAudioTranscoder> logger,
    IOptions<VoiceSttHarnessConfig> options)
{
    /// <summary>The MIME content type of the audio this transcoder produces.</summary>
    public const string OutputContentType = "audio/wav";

    private static readonly string[] _arguments =
    [
        "-hide_banner", "-loglevel", "error",
        "-i", "pipe:0",
        "-vn",
        "-ac", "1",
        "-ar", "16000",
        "-acodec", "pcm_s16le",
        "-f", "wav", "pipe:1"
    ];

    /// <summary>Converts <paramref name="audio"/> to WAV in memory.</summary>
    /// <param name="audio">The source audio bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The converted WAV bytes, or <see langword="null"/> when ffmpeg is unavailable or failed.</returns>
    public async Task<byte[]?> ToWav(byte[] audio, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(options.Value.FfmpegPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in _arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            LogFfmpegUnavailable(logger, ex.GetType().Name);
            return null;
        }

        //Drain both output pipes concurrently with the write, otherwise ffmpeg blocks on a full buffer.
        var outputTask = ReadAllBytes(process.StandardOutput.BaseStream, cancellationToken);
        var errorLengthTask = ReadLength(process.StandardError, cancellationToken);

        await using (var input = process.StandardInput.BaseStream)
            await input.WriteAsync(audio, cancellationToken);

        var output = await outputTask;
        var errorLength = await errorLengthTask;
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 || output.Length == 0)
        {
            LogTranscodeFailed(logger, process.ExitCode, output.Length, errorLength);
            return null;
        }

        return output;
    }

    #region Private helpers

    private static async Task<byte[]> ReadAllBytes(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    //Only the length is surfaced; ffmpeg diagnostics are never echoed into the log.
    private static async Task<int> ReadLength(StreamReader reader, CancellationToken cancellationToken) =>
        (await reader.ReadToEndAsync(cancellationToken)).Length;

    [LoggerMessage(LogLevel.Error, "{ClassName} could not start ffmpeg, failure={Failure}")]
    private static partial void LogFfmpegUnavailable(ILogger logger, string failure,
        string className = nameof(FfmpegAudioTranscoder));

    [LoggerMessage(LogLevel.Error,
        "{ClassName} ffmpeg conversion failed, exitCode={ExitCode}, outputBytes={OutputBytes}, errorChars={ErrorChars}")]
    private static partial void LogTranscodeFailed(ILogger logger, int exitCode, int outputBytes, int errorChars,
        string className = nameof(FfmpegAudioTranscoder));

    #endregion
}
