using System.Windows;
using MeetingTranscriber.ViewModels;

namespace MeetingTranscriber.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Auto-scroll transcript
        viewModel.Transcript.ScrollToEndRequested += (_, _) =>
        {
            if (TranscriptListBox.Items.Count > 0)
            {
                TranscriptListBox.ScrollIntoView(TranscriptListBox.Items[^1]);
            }
        };
    }
}
