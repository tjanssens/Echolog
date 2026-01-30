using System.Windows;
using MeetingTranscriber.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingTranscriber.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Auto-scroll transcript
        viewModel.Transcript.ScrollToEndRequested += (_, _) =>
        {
            if (TranscriptListBox.Items.Count > 0)
            {
                TranscriptListBox.ScrollIntoView(TranscriptListBox.Items[^1]);
            }
        };

        // Handle window opening requests
        viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
        viewModel.OpenHistoryRequested += OnOpenHistoryRequested;
    }

    private void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        var settingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        var settingsWindow = new SettingsWindow(settingsViewModel)
        {
            Owner = this
        };

        settingsWindow.ShowDialog();
    }

    private void OnOpenHistoryRequested(object? sender, EventArgs e)
    {
        var historyViewModel = App.Services.GetRequiredService<SessionHistoryViewModel>();
        var historyWindow = new SessionHistoryWindow(historyViewModel)
        {
            Owner = this
        };

        if (historyWindow.ShowDialog() == true && historyWindow.SelectedSession != null)
        {
            _viewModel.LoadSession(historyWindow.SelectedSession);
        }
    }
}
