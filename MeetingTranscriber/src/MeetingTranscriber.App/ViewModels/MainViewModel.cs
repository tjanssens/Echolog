using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Audio;
using MeetingTranscriber.Services.Storage;
using MeetingTranscriber.Services.Summary;
using MeetingTranscriber.Services.Transcription;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingTranscriber.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAudioCaptureService _audioCaptureService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ISummaryService _summaryService;
    private readonly ISessionRepository _sessionRepository;
    private readonly StorageSettings _storageSettings;
    private readonly ILogger<MainViewModel> _logger;
    private readonly DispatcherTimer _recordingTimer;

    private Session? _currentSession;
    private DateTime _recordingStartTime;

    [ObservableProperty]
    private DeviceSelectorViewModel _deviceSelector;

    [ObservableProperty]
    private TranscriptViewModel _transcript;

    [ObservableProperty]
    private SummaryViewModel _summary;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _recordingDuration = "00:00:00";

    [ObservableProperty]
    private string _statusMessage = "Klaar om te starten";

    public MainViewModel(
        IAudioCaptureService audioCaptureService,
        ITranscriptionService transcriptionService,
        ISummaryService summaryService,
        ISessionRepository sessionRepository,
        IOptions<StorageSettings> storageSettings,
        ILogger<MainViewModel> logger,
        DeviceSelectorViewModel deviceSelector,
        TranscriptViewModel transcript,
        SummaryViewModel summary)
    {
        _audioCaptureService = audioCaptureService;
        _transcriptionService = transcriptionService;
        _summaryService = summaryService;
        _sessionRepository = sessionRepository;
        _storageSettings = storageSettings.Value;
        _logger = logger;

        DeviceSelector = deviceSelector;
        Transcript = transcript;
        Summary = summary;

        _recordingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _recordingTimer.Tick += OnRecordingTimerTick;

        // Wire up events
        _audioCaptureService.AudioDataAvailable += OnAudioDataAvailable;
        _transcriptionService.TranscriptReceived += OnTranscriptReceived;
        _transcriptionService.ErrorOccurred += OnTranscriptionError;
    }

    private void OnRecordingTimerTick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _recordingStartTime;
        RecordingDuration = elapsed.ToString(@"hh\:mm\:ss");
    }

    private async void OnAudioDataAvailable(object? sender, byte[] audioData)
    {
        if (_transcriptionService.IsConnected && !IsPaused)
        {
            await _transcriptionService.SendAudioAsync(audioData);
        }
    }

    private void OnTranscriptReceived(object? sender, TranscriptSegment segment)
    {
        Transcript.AddSegment(segment);
        _currentSession?.Segments.Add(segment);
    }

    private void OnTranscriptionError(object? sender, string error)
    {
        _logger.LogError("Transcription error: {Error}", error);
        StatusMessage = $"Transcriptie fout: {error}";
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        try
        {
            StatusMessage = "Opname starten...";

            // Create new session
            _currentSession = new Session
            {
                Id = Guid.NewGuid(),
                StartTime = DateTime.Now,
                Title = $"Vergadering {DateTime.Now:yyyy-MM-dd HH:mm}"
            };

            var sessionPath = Path.Combine(
                _storageSettings.GetExpandedPath(),
                "sessions",
                _currentSession.Id.ToString());

            Directory.CreateDirectory(sessionPath);

            _currentSession.AudioInputPath = Path.Combine(sessionPath, "audio_input.wav");
            _currentSession.AudioOutputPath = Path.Combine(sessionPath, "audio_output.wav");
            _currentSession.AudioMixedPath = Path.Combine(sessionPath, "audio_mixed.wav");

            // Connect to transcription service
            await _transcriptionService.ConnectAsync();

            // Start audio capture
            await _audioCaptureService.StartCaptureAsync(sessionPath);

            // Start timer
            _recordingStartTime = DateTime.Now;
            _recordingTimer.Start();

            IsRecording = true;
            IsPaused = false;
            StatusMessage = "Opname bezig...";

            _logger.LogInformation("Recording started for session {SessionId}", _currentSession.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording");
            StatusMessage = $"Fout bij starten: {ex.Message}";
        }
    }

    private bool CanStart() => !IsRecording && DeviceSelector.HasValidSelection;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        try
        {
            StatusMessage = "Opname stoppen...";

            _recordingTimer.Stop();

            await _audioCaptureService.StopCaptureAsync();
            await _transcriptionService.DisconnectAsync();

            if (_currentSession != null)
            {
                _currentSession.EndTime = DateTime.Now;
                await _sessionRepository.CreateAsync(_currentSession);
                _logger.LogInformation("Session {SessionId} saved", _currentSession.Id);
            }

            IsRecording = false;
            IsPaused = false;
            StatusMessage = "Opname gestopt en opgeslagen";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop recording");
            StatusMessage = $"Fout bij stoppen: {ex.Message}";
        }
    }

    private bool CanStop() => IsRecording;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        if (IsPaused)
        {
            _audioCaptureService.ResumeCapture();
            IsPaused = false;
            StatusMessage = "Opname hervat";
        }
        else
        {
            _audioCaptureService.PauseCapture();
            IsPaused = true;
            StatusMessage = "Opname gepauzeerd";
        }
    }

    private bool CanPause() => IsRecording;

    [RelayCommand(CanExecute = nameof(CanGenerateSummary))]
    private async Task GenerateSummaryAsync()
    {
        try
        {
            StatusMessage = "Samenvatting genereren...";
            var summaryText = await _summaryService.GenerateSummaryAsync(Transcript.Segments.ToList());
            Summary.SummaryText = summaryText;

            if (_currentSession != null)
            {
                _currentSession.Summary = summaryText;
                await _sessionRepository.UpdateAsync(_currentSession);
            }

            StatusMessage = "Samenvatting gegenereerd";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary");
            StatusMessage = $"Fout bij samenvatting: {ex.Message}";
        }
    }

    private bool CanGenerateSummary() => !IsRecording && Transcript.Segments.Any();

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportMarkdownAsync()
    {
        try
        {
            if (_currentSession == null) return;

            var sessionPath = Path.GetDirectoryName(_currentSession.AudioInputPath);
            if (sessionPath == null) return;

            var transcriptPath = Path.Combine(sessionPath, "transcript.md");
            var markdown = Transcript.ToMarkdown(_currentSession);
            await File.WriteAllTextAsync(transcriptPath, markdown);

            if (!string.IsNullOrEmpty(Summary.SummaryText))
            {
                var summaryPath = Path.Combine(sessionPath, "summary.md");
                await File.WriteAllTextAsync(summaryPath, Summary.SummaryText);
            }

            StatusMessage = $"Geëxporteerd naar {sessionPath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export markdown");
            StatusMessage = $"Fout bij exporteren: {ex.Message}";
        }
    }

    private bool CanExport() => !IsRecording && Transcript.Segments.Any();
}
