using System.Windows;
using Microsoft.Win32;
using MeetingTranscriber.ViewModels;

namespace MeetingTranscriber.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadSettingsAsync();

        // Handle browse button click
        BrowseButton.Click += BrowseButton_Click;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Selecteer opslaglocatie",
            InitialDirectory = _viewModel.StoragePath
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.StoragePath = dialog.FolderName;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
