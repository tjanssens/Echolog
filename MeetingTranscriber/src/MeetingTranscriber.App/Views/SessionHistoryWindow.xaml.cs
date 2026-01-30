using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MeetingTranscriber.Models;
using MeetingTranscriber.ViewModels;

namespace MeetingTranscriber.Views;

public partial class SessionHistoryWindow : Window
{
    private readonly SessionHistoryViewModel _viewModel;

    public Session? SelectedSession { get; private set; }

    public SessionHistoryWindow(SessionHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.SessionSelected += (_, session) =>
        {
            SelectedSession = session;
            DialogResult = true;
            Close();
        };

        Loaded += async (_, _) => await _viewModel.LoadSessionsAsync();
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedSession != null)
        {
            _viewModel.OpenSessionCommand.Execute(null);
        }
    }
}
