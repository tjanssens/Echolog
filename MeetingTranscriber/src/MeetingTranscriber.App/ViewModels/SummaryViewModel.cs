using CommunityToolkit.Mvvm.ComponentModel;

namespace MeetingTranscriber.ViewModels;

public partial class SummaryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    public void Clear()
    {
        SummaryText = string.Empty;
    }
}
