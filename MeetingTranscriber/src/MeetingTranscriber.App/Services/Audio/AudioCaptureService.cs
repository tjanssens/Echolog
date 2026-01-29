using MeetingTranscriber.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingTranscriber.Services.Audio;

public class AudioCaptureService : IAudioCaptureService, IDisposable
{
    private readonly AudioSettings _settings;
    private readonly ILogger<AudioCaptureService> _logger;

    private string? _selectedInputDeviceId;
    private string? _selectedOutputDeviceId;
    private bool _isCapturing;
    private bool _isPaused;
    private bool _disposed;

    public bool IsCapturing => _isCapturing;
    public bool IsPaused => _isPaused;

    public event EventHandler<byte[]>? AudioDataAvailable;
    public event EventHandler<float>? InputLevelChanged;
    public event EventHandler<float>? OutputLevelChanged;

    public AudioCaptureService(
        IOptions<AudioSettings> settings,
        ILogger<AudioCaptureService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IReadOnlyList<AudioDevice> GetInputDevices()
    {
        var devices = new List<AudioDevice>();

        try
        {
            // NAudio implementation will be added in Phase 2
            // For now, return placeholder devices for testing
            devices.Add(new AudioDevice("input-0", "Default Microphone", AudioDeviceType.Input));

            // In real implementation:
            // for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            // {
            //     var caps = WaveInEvent.GetCapabilities(i);
            //     devices.Add(new AudioDevice($"input-{i}", caps.ProductName, AudioDeviceType.Input));
            // }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate input devices");
        }

        return devices;
    }

    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        var devices = new List<AudioDevice>();

        try
        {
            // NAudio implementation will be added in Phase 2
            // For now, return placeholder devices for testing
            devices.Add(new AudioDevice("output-0", "Default Speakers", AudioDeviceType.Output));

            // In real implementation using WASAPI:
            // var enumerator = new MMDeviceEnumerator();
            // foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            // {
            //     devices.Add(new AudioDevice(device.ID, device.FriendlyName, AudioDeviceType.Output));
            // }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate output devices");
        }

        return devices;
    }

    public void SetInputDevice(string deviceId)
    {
        _selectedInputDeviceId = deviceId;
        _logger.LogDebug("Selected input device: {DeviceId}", deviceId);
    }

    public void SetOutputDevice(string deviceId)
    {
        _selectedOutputDeviceId = deviceId;
        _logger.LogDebug("Selected output device: {DeviceId}", deviceId);
    }

    public Task StartCaptureAsync(string sessionPath)
    {
        if (_isCapturing)
        {
            throw new InvalidOperationException("Capture is already in progress");
        }

        if (string.IsNullOrEmpty(_selectedInputDeviceId) || string.IsNullOrEmpty(_selectedOutputDeviceId))
        {
            throw new InvalidOperationException("Input and output devices must be selected before starting capture");
        }

        _logger.LogInformation("Starting audio capture to {SessionPath}", sessionPath);

        // NAudio capture implementation will be added in Phase 2
        // This will include:
        // - WaveInEvent for microphone capture
        // - WasapiLoopbackCapture for system audio
        // - WaveFileWriter for saving WAV files
        // - Resampling to 16kHz mono for Deepgram

        _isCapturing = true;
        _isPaused = false;

        return Task.CompletedTask;
    }

    public Task StopCaptureAsync()
    {
        if (!_isCapturing)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Stopping audio capture");

        // Stop and dispose NAudio resources in Phase 2

        _isCapturing = false;
        _isPaused = false;

        return Task.CompletedTask;
    }

    public void PauseCapture()
    {
        if (_isCapturing && !_isPaused)
        {
            _isPaused = true;
            _logger.LogDebug("Audio capture paused");
        }
    }

    public void ResumeCapture()
    {
        if (_isCapturing && _isPaused)
        {
            _isPaused = false;
            _logger.LogDebug("Audio capture resumed");
        }
    }

    protected void OnAudioDataAvailable(byte[] data)
    {
        AudioDataAvailable?.Invoke(this, data);
    }

    protected void OnInputLevelChanged(float level)
    {
        InputLevelChanged?.Invoke(this, level);
    }

    protected void OnOutputLevelChanged(float level)
    {
        OutputLevelChanged?.Invoke(this, level);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_isCapturing)
        {
            StopCaptureAsync().Wait();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
