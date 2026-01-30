using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Settings;
using Microsoft.Extensions.Logging;

namespace MeetingTranscriber.Services.Transcription;

public class DeepgramTranscriptionService : ITranscriptionService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DeepgramTranscriptionService> _logger;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private bool _isConnected;
    private bool _disposed;
    private int _speakerCounter = 0;
    private int _audioPacketCounter = 0;

    public bool IsConnected => _isConnected;

    public event EventHandler<TranscriptSegment>? TranscriptReceived;
    public event EventHandler<string>? ErrorOccurred;

    public DeepgramTranscriptionService(
        ISettingsService settingsService,
        ILogger<DeepgramTranscriptionService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task ConnectAsync()
    {
        if (_isConnected)
        {
            return;
        }

        var settings = await _settingsService.GetSettingsAsync();

        if (string.IsNullOrEmpty(settings.DeepgramApiKey))
        {
            var error = "Deepgram API key is niet geconfigureerd. Ga naar Instellingen om de API key in te voeren.";
            _logger.LogWarning(error);
            OnErrorOccurred(error);
            return;
        }

        _logger.LogInformation("Connecting to Deepgram with model {Model}, language {Language}",
            settings.DeepgramModel, settings.DeepgramLanguage);

        try
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", $"Token {settings.DeepgramApiKey}");

            // Build Deepgram WebSocket URL with options
            var uri = new Uri(
                $"wss://api.deepgram.com/v1/listen?" +
                $"model={settings.DeepgramModel}&" +
                $"language={settings.DeepgramLanguage}&" +
                $"smart_format=true&" +
                $"diarize=true&" +
                $"punctuate=true&" +
                $"encoding=linear16&" +
                $"sample_rate=16000&" +
                $"channels=1&" +
                $"interim_results=true&" +
                $"utterance_end_ms=1000"
            );

            await _webSocket.ConnectAsync(uri, CancellationToken.None);

            _isConnected = true;
            _speakerCounter = 0;

            // Start receiving messages
            _receiveCts = new CancellationTokenSource();
            _receiveTask = ReceiveMessagesAsync(_receiveCts.Token);

            _logger.LogInformation("Connected to Deepgram successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Deepgram");
            OnErrorOccurred($"Verbinding met Deepgram mislukt: {ex.Message}");
            _isConnected = false;
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Deepgram WebSocket closed by server");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogDebug("Deepgram message received: {Message}", message.Length > 200 ? message[..200] + "..." : message);
                    ProcessDeepgramMessage(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving Deepgram messages");
            OnErrorOccurred($"Fout bij ontvangen transcriptie: {ex.Message}");
        }
    }

    private void ProcessDeepgramMessage(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            // Check for error
            if (root.TryGetProperty("error", out var error))
            {
                var errorMessage = error.GetString() ?? "Unknown error";
                _logger.LogError("Deepgram error: {Error}", errorMessage);
                OnErrorOccurred(errorMessage);
                return;
            }

            // Check for transcript
            if (root.TryGetProperty("channel", out var channel))
            {
                var alternatives = channel.GetProperty("alternatives");
                if (alternatives.GetArrayLength() > 0)
                {
                    var firstAlt = alternatives[0];
                    var transcript = firstAlt.GetProperty("transcript").GetString();

                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        var isFinal = root.GetProperty("is_final").GetBoolean();
                        var speaker = "Speaker 1";

                        // Get speaker from diarization if available
                        if (firstAlt.TryGetProperty("words", out var words) && words.GetArrayLength() > 0)
                        {
                            var firstWord = words[0];
                            if (firstWord.TryGetProperty("speaker", out var speakerProp))
                            {
                                var speakerNum = speakerProp.GetInt32();
                                speaker = $"Speaker {speakerNum + 1}";
                            }
                        }

                        var segment = new TranscriptSegment(
                            DateTime.Now,
                            speaker,
                            transcript,
                            isFinal,
                            AudioSource.Microphone
                        );

                        OnTranscriptReceived(segment);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Deepgram message: {Message}", message);
        }
    }

    public async Task DisconnectAsync()
    {
        if (!_isConnected)
        {
            return;
        }

        _logger.LogInformation("Disconnecting from Deepgram");

        try
        {
            // Cancel receive loop
            _receiveCts?.Cancel();

            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Receive task did not complete in time");
                }
            }

            // Close WebSocket
            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Deepgram disconnect");
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _receiveCts?.Dispose();
            _receiveCts = null;
            _receiveTask = null;
            _isConnected = false;
        }
    }

    public async Task SendAudioAsync(byte[] audioData)
    {
        if (!_isConnected || _webSocket?.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            await _webSocket.SendAsync(
                new ArraySegment<byte>(audioData),
                WebSocketMessageType.Binary,
                true,
                CancellationToken.None
            );

            _audioPacketCounter++;
            if (_audioPacketCounter % 50 == 0)
            {
                _logger.LogDebug("Sent {Count} audio packets to Deepgram ({Bytes} bytes last)",
                    _audioPacketCounter, audioData.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio to Deepgram");
        }
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
