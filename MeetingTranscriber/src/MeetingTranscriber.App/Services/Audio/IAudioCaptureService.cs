using MeetingTranscriber.Models;

namespace MeetingTranscriber.Services.Audio;

public interface IAudioCaptureService
{
    IReadOnlyList<AudioDevice> GetInputDevices();
    IReadOnlyList<AudioDevice> GetOutputDevices();

    void SetInputDevice(string deviceId);
    void SetOutputDevice(string deviceId);

    Task StartCaptureAsync(string sessionPath);
    Task StopCaptureAsync();
    void PauseCapture();
    void ResumeCapture();

    bool IsCapturing { get; }
    bool IsPaused { get; }

    event EventHandler<byte[]>? AudioDataAvailable;
    event EventHandler<float>? InputLevelChanged;
    event EventHandler<float>? OutputLevelChanged;
}
