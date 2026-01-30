using System.IO;
using MeetingTranscriber.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MeetingTranscriber.Services.Audio;

public class AudioCaptureService : IAudioCaptureService, IDisposable
{
    private readonly AudioSettings _settings;
    private readonly ILogger<AudioCaptureService> _logger;

    private string? _selectedInputDeviceId;
    private string? _selectedOutputDeviceId;
    private int _selectedInputDeviceIndex;
    private MMDevice? _selectedOutputDevice;

    private WaveInEvent? _microphoneCapture;
    private WasapiLoopbackCapture? _loopbackCapture;
    private WaveFileWriter? _microphoneWriter;
    private WaveFileWriter? _loopbackWriter;
    private WaveFileWriter? _mixedWriter;

    private WaveFormat? _targetFormat;

    private bool _isCapturing;
    private bool _isPaused;
    private bool _disposed;

    private readonly object _lockObject = new();

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
        _targetFormat = new WaveFormat(_settings.SampleRate, _settings.BitsPerSample, _settings.Channels);
    }

    public IReadOnlyList<AudioDevice> GetInputDevices()
    {
        var devices = new List<AudioDevice>();

        try
        {
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                devices.Add(new AudioDevice($"input-{i}", caps.ProductName, AudioDeviceType.Input));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate input devices");
        }

        if (devices.Count == 0)
        {
            devices.Add(new AudioDevice("input-0", "Default Microphone (Not Available)", AudioDeviceType.Input));
        }

        return devices;
    }

    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        var devices = new List<AudioDevice>();

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                devices.Add(new AudioDevice(device.ID, device.FriendlyName, AudioDeviceType.Output));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate output devices");
        }

        if (devices.Count == 0)
        {
            devices.Add(new AudioDevice("output-0", "Default Speakers (Not Available)", AudioDeviceType.Output));
        }

        return devices;
    }

    public void SetInputDevice(string deviceId)
    {
        _selectedInputDeviceId = deviceId;

        if (deviceId.StartsWith("input-") && int.TryParse(deviceId.AsSpan(6), out int index))
        {
            _selectedInputDeviceIndex = index;
        }

        _logger.LogDebug("Selected input device: {DeviceId}", deviceId);
    }

    public void SetOutputDevice(string deviceId)
    {
        _selectedOutputDeviceId = deviceId;

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            _selectedOutputDevice = enumerator.GetDevice(deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get output device {DeviceId}", deviceId);
        }

        _logger.LogDebug("Selected output device: {DeviceId}", deviceId);
    }

    public async Task StartCaptureAsync(string sessionPath)
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

        Directory.CreateDirectory(sessionPath);

        var inputPath = Path.Combine(sessionPath, "audio_input.wav");
        var outputPath = Path.Combine(sessionPath, "audio_output.wav");
        var mixedPath = Path.Combine(sessionPath, "audio_mixed.wav");

        try
        {
            // Setup microphone capture at target format directly
            _microphoneCapture = new WaveInEvent
            {
                DeviceNumber = _selectedInputDeviceIndex,
                WaveFormat = _targetFormat
            };

            _microphoneWriter = new WaveFileWriter(inputPath, _targetFormat!);

            _microphoneCapture.DataAvailable += OnMicrophoneDataAvailable;
            _microphoneCapture.RecordingStopped += OnRecordingStopped;

            // Setup loopback capture
            if (_selectedOutputDevice != null)
            {
                _loopbackCapture = new WasapiLoopbackCapture(_selectedOutputDevice);
                _loopbackWriter = new WaveFileWriter(outputPath, _targetFormat!);

                _loopbackCapture.DataAvailable += OnLoopbackDataAvailable;
                _loopbackCapture.RecordingStopped += OnRecordingStopped;
            }

            // Setup mixed writer
            _mixedWriter = new WaveFileWriter(mixedPath, _targetFormat!);

            // Start capturing
            _microphoneCapture.StartRecording();
            _loopbackCapture?.StartRecording();

            _isCapturing = true;
            _isPaused = false;

            _logger.LogInformation("Audio capture started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio capture");
            await StopCaptureAsync();
            throw;
        }

        await Task.CompletedTask;
    }

    private void OnMicrophoneDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_isPaused || !_isCapturing) return;

        lock (_lockObject)
        {
            try
            {
                // Calculate level
                float maxLevel = CalculateLevel(e.Buffer, e.BytesRecorded);
                InputLevelChanged?.Invoke(this, maxLevel);

                // Write to file
                _microphoneWriter?.Write(e.Buffer, 0, e.BytesRecorded);

                // Write to mixed and send for transcription
                _mixedWriter?.Write(e.Buffer, 0, e.BytesRecorded);

                // Copy data for event
                var audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                AudioDataAvailable?.Invoke(this, audioData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing microphone data");
            }
        }
    }

    private void OnLoopbackDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_isPaused || !_isCapturing) return;

        lock (_lockObject)
        {
            try
            {
                // Calculate level from float samples (WASAPI returns float)
                float maxLevel = CalculateLevelFloat(e.Buffer, e.BytesRecorded, _loopbackCapture!.WaveFormat);
                OutputLevelChanged?.Invoke(this, maxLevel);

                // Convert and resample loopback audio to target format
                var converted = ConvertLoopbackToTarget(e.Buffer, e.BytesRecorded, _loopbackCapture.WaveFormat);
                if (converted != null && converted.Length > 0)
                {
                    _loopbackWriter?.Write(converted, 0, converted.Length);

                    // Also send loopback audio for transcription
                    AudioDataAvailable?.Invoke(this, converted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing loopback data");
            }
        }
    }

    private byte[]? ConvertLoopbackToTarget(byte[] buffer, int bytesRecorded, WaveFormat sourceFormat)
    {
        // WASAPI loopback is typically 32-bit float, stereo, 48kHz
        // We need to convert to 16-bit, mono, 16kHz

        if (sourceFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            int sourceSamples = bytesRecorded / (sourceFormat.BitsPerSample / 8);
            int sourceChannels = sourceFormat.Channels;
            int framesCount = sourceSamples / sourceChannels;

            // Calculate output size (mono, 16-bit, potentially different sample rate)
            double ratio = (double)_targetFormat!.SampleRate / sourceFormat.SampleRate;
            int outputFrames = (int)(framesCount * ratio);
            var output = new byte[outputFrames * 2]; // 16-bit mono = 2 bytes per sample

            int outputIndex = 0;
            for (int i = 0; i < outputFrames && outputIndex < output.Length - 1; i++)
            {
                int sourceFrame = (int)(i / ratio);
                if (sourceFrame >= framesCount) sourceFrame = framesCount - 1;

                // Mix channels to mono
                float sample = 0;
                for (int ch = 0; ch < sourceChannels; ch++)
                {
                    int byteOffset = (sourceFrame * sourceChannels + ch) * 4;
                    if (byteOffset + 3 < bytesRecorded)
                    {
                        sample += BitConverter.ToSingle(buffer, byteOffset);
                    }
                }
                sample /= sourceChannels;

                // Convert to 16-bit
                short sample16 = (short)(sample * 32767);
                output[outputIndex++] = (byte)(sample16 & 0xFF);
                output[outputIndex++] = (byte)((sample16 >> 8) & 0xFF);
            }

            return output;
        }

        return null;
    }

    private float CalculateLevel(byte[] buffer, int bytesRecorded)
    {
        float max = 0;
        for (int i = 0; i < bytesRecorded - 1; i += 2)
        {
            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            float sample32 = Math.Abs(sample / 32768f);
            if (sample32 > max) max = sample32;
        }
        return max;
    }

    private float CalculateLevelFloat(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        if (format.Encoding != WaveFormatEncoding.IeeeFloat) return 0;

        float max = 0;
        for (int i = 0; i < bytesRecorded - 3; i += 4)
        {
            float sample = Math.Abs(BitConverter.ToSingle(buffer, i));
            if (sample > max) max = sample;
        }
        return Math.Min(max, 1.0f);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            _logger.LogError(e.Exception, "Recording stopped due to error");
        }
    }

    public async Task StopCaptureAsync()
    {
        if (!_isCapturing)
        {
            return;
        }

        _logger.LogInformation("Stopping audio capture");

        lock (_lockObject)
        {
            _isCapturing = false;

            try
            {
                _microphoneCapture?.StopRecording();
                _loopbackCapture?.StopRecording();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping recording");
            }

            // Dispose writers
            _microphoneWriter?.Dispose();
            _microphoneWriter = null;

            _loopbackWriter?.Dispose();
            _loopbackWriter = null;

            _mixedWriter?.Dispose();
            _mixedWriter = null;

            // Dispose capture devices
            if (_microphoneCapture != null)
            {
                _microphoneCapture.DataAvailable -= OnMicrophoneDataAvailable;
                _microphoneCapture.RecordingStopped -= OnRecordingStopped;
                _microphoneCapture.Dispose();
                _microphoneCapture = null;
            }

            if (_loopbackCapture != null)
            {
                _loopbackCapture.DataAvailable -= OnLoopbackDataAvailable;
                _loopbackCapture.RecordingStopped -= OnRecordingStopped;
                _loopbackCapture.Dispose();
                _loopbackCapture = null;
            }
        }

        _isPaused = false;
        _logger.LogInformation("Audio capture stopped");

        await Task.CompletedTask;
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

    public void Dispose()
    {
        if (_disposed) return;

        if (_isCapturing)
        {
            StopCaptureAsync().Wait();
        }

        _selectedOutputDevice?.Dispose();
        _selectedOutputDevice = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
