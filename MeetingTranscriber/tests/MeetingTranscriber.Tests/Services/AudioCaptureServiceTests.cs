using FluentAssertions;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MeetingTranscriber.Tests.Services;

public class AudioCaptureServiceTests
{
    private readonly Mock<IOptions<AudioSettings>> _settingsMock;
    private readonly Mock<ILogger<AudioCaptureService>> _loggerMock;
    private readonly AudioCaptureService _service;

    public AudioCaptureServiceTests()
    {
        _settingsMock = new Mock<IOptions<AudioSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(new AudioSettings
        {
            SampleRate = 16000,
            Channels = 1,
            BitsPerSample = 16
        });

        _loggerMock = new Mock<ILogger<AudioCaptureService>>();

        _service = new AudioCaptureService(_settingsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GetInputDevices_ShouldReturnAtLeastOneDevice()
    {
        // Act
        var devices = _service.GetInputDevices();

        // Assert
        devices.Should().NotBeNull();
        devices.Should().HaveCountGreaterThan(0);
        devices.Should().AllSatisfy(d => d.Type.Should().Be(AudioDeviceType.Input));
    }

    [Fact]
    public void GetOutputDevices_ShouldReturnAtLeastOneDevice()
    {
        // Act
        var devices = _service.GetOutputDevices();

        // Assert
        devices.Should().NotBeNull();
        devices.Should().HaveCountGreaterThan(0);
        devices.Should().AllSatisfy(d => d.Type.Should().Be(AudioDeviceType.Output));
    }

    [Fact]
    public void SetInputDevice_ShouldNotThrow()
    {
        // Act
        var action = () => _service.SetInputDevice("test-device-id");

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void SetOutputDevice_ShouldNotThrow()
    {
        // Act
        var action = () => _service.SetOutputDevice("test-device-id");

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void IsCapturing_ShouldBeFalseInitially()
    {
        // Assert
        _service.IsCapturing.Should().BeFalse();
    }

    [Fact]
    public void IsPaused_ShouldBeFalseInitially()
    {
        // Assert
        _service.IsPaused.Should().BeFalse();
    }

    [Fact]
    public async Task StartCaptureAsync_WithoutDevices_ShouldThrowInvalidOperationException()
    {
        // Act
        var action = async () => await _service.StartCaptureAsync("/tmp/test");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*devices must be selected*");
    }

    [Fact]
    public async Task StartCaptureAsync_WithDevices_ShouldSetIsCapturingTrue()
    {
        // Arrange
        _service.SetInputDevice("input-0");
        _service.SetOutputDevice("output-0");

        // Act
        await _service.StartCaptureAsync("/tmp/test");

        // Assert
        _service.IsCapturing.Should().BeTrue();
    }

    [Fact]
    public async Task StopCaptureAsync_WhenCapturing_ShouldSetIsCapturingFalse()
    {
        // Arrange
        _service.SetInputDevice("input-0");
        _service.SetOutputDevice("output-0");
        await _service.StartCaptureAsync("/tmp/test");

        // Act
        await _service.StopCaptureAsync();

        // Assert
        _service.IsCapturing.Should().BeFalse();
    }

    [Fact]
    public async Task PauseCapture_WhenCapturing_ShouldSetIsPausedTrue()
    {
        // Arrange
        _service.SetInputDevice("input-0");
        _service.SetOutputDevice("output-0");
        await _service.StartCaptureAsync("/tmp/test");

        // Act
        _service.PauseCapture();

        // Assert
        _service.IsPaused.Should().BeTrue();
    }

    [Fact]
    public async Task ResumeCapture_WhenPaused_ShouldSetIsPausedFalse()
    {
        // Arrange
        _service.SetInputDevice("input-0");
        _service.SetOutputDevice("output-0");
        await _service.StartCaptureAsync("/tmp/test");
        _service.PauseCapture();

        // Act
        _service.ResumeCapture();

        // Assert
        _service.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void PauseCapture_WhenNotCapturing_ShouldNotChangeState()
    {
        // Act
        _service.PauseCapture();

        // Assert
        _service.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act
        var action = () => _service.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_WhenCapturing_ShouldStopCapture()
    {
        // Arrange
        _service.SetInputDevice("input-0");
        _service.SetOutputDevice("output-0");
        await _service.StartCaptureAsync("/tmp/test");

        // Act
        _service.Dispose();

        // Assert
        _service.IsCapturing.Should().BeFalse();
    }
}
