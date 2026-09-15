namespace CasCap.Samples;

/// <summary>The speech-to-text server the voice acceptance harness talks to.</summary>
public enum VoiceSttProvider
{
    /// <summary>Stock whisper.cpp: multipart <c>POST /inference</c> with a <c>file</c> part.</summary>
    WhisperCpp,

    /// <summary>openai-whisper-asr-webservice: multipart <c>POST /asr</c> with an <c>audio_file</c> part.</summary>
    WhisperAsr
}
