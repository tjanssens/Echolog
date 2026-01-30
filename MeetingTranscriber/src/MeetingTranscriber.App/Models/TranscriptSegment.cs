namespace MeetingTranscriber.Models;

public enum AudioSource
{
    Microphone,
    SystemAudio
}

public record TranscriptSegment(
    DateTime Timestamp,
    string Speaker,
    string Text,
    bool IsFinal,
    AudioSource Source
);
