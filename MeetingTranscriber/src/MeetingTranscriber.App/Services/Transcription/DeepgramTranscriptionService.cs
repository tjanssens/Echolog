using MeetingTranscriber.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingTranscriber.Services.Transcription;

public class DeepgramTranscriptionService : ITranscriptionService, IDisposable
{
    private readonly DeepgramSettings _settings;
    private readonly ILogger<DeepgramTranscriptionService> _logger;

    private bool _isConnected;
    private bool _disposed;

    public bool IsConnected => _isConnected;

    public event EventHandler<TranscriptSegment>? TranscriptReceived;
    public event EventHandler<string>? ErrorOccurred;

    public DeepgramTranscriptionService(
        IOptions<DeepgramSettings> settings,
        ILogger<DeepgramTranscriptionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ConnectAsync()
    {
        if (_isConnected)
        {
            return;
        }

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            var error = "Deepgram API key is not configured";
            _logger.LogWarning(error);
            OnErrorOccurred(error);
            // Continue without transcription for testing
        }

        _logger.LogInformation("Connecting to Deepgram with model {Model}, language {Language}",
            _settings.Model, _settings.Language);

        // Deepgram SDK implementation will be added in Phase 4
        // This will include:
        // - DeepgramClient initialization
        // - LiveTranscriptionOptions configuration
        // - WebSocket connection to Deepgram
        // - Event handlers for transcript chunks

        _isConnected = true;
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        if (!_isConnected)
        {
            return;
        }

        _logger.LogInformation("Disconnecting from Deepgram");

        // Close WebSocket connection in Phase 4

        _isConnected = false;
        await Task.CompletedTask;
    }

    public async Task SendAudioAsync(byte[] audioData)
    {
        if (!_isConnected)
        {
            return;
        }

        // Send audio data over WebSocket in Phase 4
        // The audio should be 16kHz, 16-bit, mono PCM

        await Task.CompletedTask;
    }

    protected void OnTranscriptReceived(TranscriptSegment segment)
    {
        TranscriptReceived?.Invoke(this, segment);
    }

    protected void OnErrorOccurred(string error)
    {
        ErrorOccurred?.Invoke(this, error);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_isConnected)
        {
            DisconnectAsync().Wait();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
