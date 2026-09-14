namespace CasCap.Samples.Abstractions;

/// <summary>A replaceable speech-to-text boundary used only by the voice acceptance harness.</summary>
/// <remarks>
/// Keeps the speech-to-text backend out of the published Signal client package. The final application
/// implementation is expected to use a shared abstraction rather than this sample-only interface.
/// </remarks>
public interface IVoiceTranscriber
{
    /// <summary>Transcribes a complete audio payload.</summary>
    /// <param name="audio">The audio bytes to transcribe.</param>
    /// <param name="contentType">The MIME content type of <paramref name="audio"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw transcript, or <see langword="null"/> when the endpoint returned no usable text.</returns>
    /// <remarks>Implementations must never log, persist or echo the transcript or the audio.</remarks>
    Task<string?> Transcribe(byte[] audio, string contentType, CancellationToken cancellationToken = default);
}
