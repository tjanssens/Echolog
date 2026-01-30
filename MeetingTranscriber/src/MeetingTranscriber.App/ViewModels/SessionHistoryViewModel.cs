using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Storage;
using Microsoft.Extensions.Logging;

namespace MeetingTranscriber.ViewModels;

public partial class SessionHistoryViewModel : ObservableObject
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ILogger<SessionHistoryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SessionListItem> _sessions = new();

    [ObservableProperty]
    private SessionListItem? _selectedSession;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event EventHandler<Session>? SessionSelected;

    public SessionHistoryViewModel(
        ISessionRepository sessionRepository,
        ILogger<SessionHistoryViewModel> logger)
    {
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    public async Task LoadSessionsAsync()
    {
        IsLoading = true;
        StatusMessage = "Sessies laden...";

        try
        {
            var sessions = await _sessionRepository.GetAllAsync();

            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(new SessionListItem(session));
            }

            StatusMessage = $"{Sessions.Count} sessie(s) gevonden";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions");
            StatusMessage = "Fout bij laden sessies";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenSession()
    {
        if (SelectedSession != null)
        {
            SessionSelected?.Invoke(this, SelectedSession.Session);
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (SelectedSession == null) return;

        var path = Path.GetDirectoryName(SelectedSession.Session.AudioInputPath);
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private async Task DeleteSessionAsync()
    {
        if (SelectedSession == null) return;

        try
        {
            await _sessionRepository.DeleteAsync(SelectedSession.Session.Id);
            Sessions.Remove(SelectedSession);
            SelectedSession = null;
            StatusMessage = "Sessie verwijderd";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session");
            StatusMessage = "Fout bij verwijderen sessie";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadSessionsAsync();
    }
}

public partial class SessionListItem : ObservableObject
{
    public Session Session { get; }

    public string Title => string.IsNullOrEmpty(Session.Title)
        ? $"Sessie {Session.StartTime:dd-MM-yyyy HH:mm}"
        : Session.Title;

    public string DateDisplay => Session.StartTime.ToString("dd MMMM yyyy");
    public string TimeDisplay => Session.StartTime.ToString("HH:mm");

    public string Duration
    {
        get
        {
            if (Session.EndTime.HasValue)
            {
                var duration = Session.EndTime.Value - Session.StartTime;
                return duration.TotalHours >= 1
                    ? $"{duration:h\\:mm\\:ss}"
                    : $"{duration:mm\\:ss}";
            }
            return "--:--";
        }
    }

    public int SegmentCount => Session.Segments.Count(s => s.IsFinal);
    public bool HasSummary => !string.IsNullOrEmpty(Session.Summary);

    public SessionListItem(Session session)
    {
        Session = session;
    }
}
