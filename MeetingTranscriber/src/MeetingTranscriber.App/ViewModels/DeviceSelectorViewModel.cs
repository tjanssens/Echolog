using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Audio;

namespace MeetingTranscriber.ViewModels;

public partial class DeviceSelectorViewModel : ObservableObject
{
    private readonly IAudioCaptureService _audioCaptureService;

    [ObservableProperty]
    private ObservableCollection<AudioDevice> _inputDevices = new();

    [ObservableProperty]
    private ObservableCollection<AudioDevice> _outputDevices = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidSelection))]
    private AudioDevice? _selectedInputDevice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidSelection))]
    private AudioDevice? _selectedOutputDevice;

    [ObservableProperty]
    private float _inputLevel;

    [ObservableProperty]
    private float _outputLevel;

    public bool HasValidSelection => SelectedInputDevice != null && SelectedOutputDevice != null;

    public DeviceSelectorViewModel(IAudioCaptureService audioCaptureService)
    {
        _audioCaptureService = audioCaptureService;

        _audioCaptureService.InputLevelChanged += (_, level) => InputLevel = level;
        _audioCaptureService.OutputLevelChanged += (_, level) => OutputLevel = level;

        RefreshDevices();
    }

    public void RefreshDevices()
    {
        InputDevices.Clear();
        foreach (var device in _audioCaptureService.GetInputDevices())
        {
            InputDevices.Add(device);
        }

        OutputDevices.Clear();
        foreach (var device in _audioCaptureService.GetOutputDevices())
        {
            OutputDevices.Add(device);
        }

        // Select first devices by default
        if (InputDevices.Any() && SelectedInputDevice == null)
        {
            SelectedInputDevice = InputDevices.First();
        }

        if (OutputDevices.Any() && SelectedOutputDevice == null)
        {
            SelectedOutputDevice = OutputDevices.First();
        }
    }

    partial void OnSelectedInputDeviceChanged(AudioDevice? value)
    {
        if (value != null)
        {
            _audioCaptureService.SetInputDevice(value.Id);
        }
    }

    partial void OnSelectedOutputDeviceChanged(AudioDevice? value)
    {
        if (value != null)
        {
            _audioCaptureService.SetOutputDevice(value.Id);
        }
    }
}
