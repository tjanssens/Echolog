using MeetingTranscriber.Models;

namespace MeetingTranscriber.Services.Transcription;

public interface ITranscriptionService
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SendAudioAsync(byte[] audioData);

    bool IsConnected { get; }

    event EventHandler<TranscriptSegment>? TranscriptReceived;
    event EventHandler<string>? ErrorOccurred;
}
