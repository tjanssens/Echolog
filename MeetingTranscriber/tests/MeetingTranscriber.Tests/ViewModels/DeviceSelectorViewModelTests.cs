using FluentAssertions;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Audio;
using MeetingTranscriber.ViewModels;
using Moq;
using Xunit;

namespace MeetingTranscriber.Tests.ViewModels;

public class DeviceSelectorViewModelTests
{
    private readonly Mock<IAudioCaptureService> _audioCaptureServiceMock;

    public DeviceSelectorViewModelTests()
    {
        _audioCaptureServiceMock = new Mock<IAudioCaptureService>();

        _audioCaptureServiceMock.Setup(x => x.GetInputDevices()).Returns(new List<AudioDevice>
        {
            new("input-1", "Microphone 1", AudioDeviceType.Input),
            new("input-2", "Microphone 2", AudioDeviceType.Input)
        });

        _audioCaptureServiceMock.Setup(x => x.GetOutputDevices()).Returns(new List<AudioDevice>
        {
            new("output-1", "Speakers 1", AudioDeviceType.Output),
            new("output-2", "Speakers 2", AudioDeviceType.Output)
        });
    }

    [Fact]
    public void Constructor_ShouldLoadDevices()
    {
        // Act
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);

        // Assert
        viewModel.InputDevices.Should().HaveCount(2);
        viewModel.OutputDevices.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_ShouldSelectFirstDevicesByDefault()
    {
        // Act
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);

        // Assert
        viewModel.SelectedInputDevice.Should().NotBeNull();
        viewModel.SelectedInputDevice!.Id.Should().Be("input-1");
        viewModel.SelectedOutputDevice.Should().NotBeNull();
        viewModel.SelectedOutputDevice!.Id.Should().Be("output-1");
    }

    [Fact]
    public void HasValidSelection_WithBothDevicesSelected_ShouldBeTrue()
    {
        // Arrange
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);

        // Assert
        viewModel.HasValidSelection.Should().BeTrue();
    }

    [Fact]
    public void HasValidSelection_WithNoDevicesSelected_ShouldBeFalse()
    {
        // Arrange
        _audioCaptureServiceMock.Setup(x => x.GetInputDevices()).Returns(new List<AudioDevice>());
        _audioCaptureServiceMock.Setup(x => x.GetOutputDevices()).Returns(new List<AudioDevice>());

        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);

        // Assert
        viewModel.HasValidSelection.Should().BeFalse();
    }

    [Fact]
    public void SelectedInputDevice_WhenChanged_ShouldCallSetInputDevice()
    {
        // Arrange
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);
        var newDevice = new AudioDevice("input-2", "Microphone 2", AudioDeviceType.Input);

        // Act
        viewModel.SelectedInputDevice = newDevice;

        // Assert
        _audioCaptureServiceMock.Verify(x => x.SetInputDevice("input-2"), Times.Once);
    }

    [Fact]
    public void SelectedOutputDevice_WhenChanged_ShouldCallSetOutputDevice()
    {
        // Arrange
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);
        var newDevice = new AudioDevice("output-2", "Speakers 2", AudioDeviceType.Output);

        // Act
        viewModel.SelectedOutputDevice = newDevice;

        // Assert
        _audioCaptureServiceMock.Verify(x => x.SetOutputDevice("output-2"), Times.Once);
    }

    [Fact]
    public void RefreshDevices_ShouldReloadDevices()
    {
        // Arrange
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);

        _audioCaptureServiceMock.Setup(x => x.GetInputDevices()).Returns(new List<AudioDevice>
        {
            new("input-3", "New Microphone", AudioDeviceType.Input)
        });

        // Act
        viewModel.RefreshDevices();

        // Assert
        viewModel.InputDevices.Should().ContainSingle();
        viewModel.InputDevices[0].Name.Should().Be("New Microphone");
    }

    [Fact]
    public void InputLevel_ShouldBeUpdatedFromEvent()
    {
        // Arrange
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);

        // Act
        _audioCaptureServiceMock.Raise(x => x.InputLevelChanged += null, this, 0.75f);

        // Assert
        viewModel.InputLevel.Should().Be(0.75f);
    }

    [Fact]
    public void OutputLevel_ShouldBeUpdatedFromEvent()
    {
        // Arrange
        var viewModel = new DeviceSelectorViewModel(_audioCaptureServiceMock.Object);

        // Act
        _audioCaptureServiceMock.Raise(x => x.OutputLevelChanged += null, this, 0.5f);

        // Assert
        viewModel.OutputLevel.Should().Be(0.5f);
    }
}
