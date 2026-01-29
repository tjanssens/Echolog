using FluentAssertions;
using MeetingTranscriber.Services.Transcription;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MeetingTranscriber.Tests.Services;

public class DeepgramTranscriptionServiceTests
{
    private readonly Mock<IOptions<DeepgramSettings>> _settingsMock;
    private readonly Mock<ILogger<DeepgramTranscriptionService>> _loggerMock;
    private readonly DeepgramTranscriptionService _service;

    public DeepgramTranscriptionServiceTests()
    {
        _settingsMock = new Mock<IOptions<DeepgramSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(new DeepgramSettings
        {
            ApiKey = "test-api-key",
            Language = "nl",
            Model = "nova-2"
        });

        _loggerMock = new Mock<ILogger<DeepgramTranscriptionService>>();

        _service = new DeepgramTranscriptionService(_settingsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void IsConnected_ShouldBeFalseInitially()
    {
        // Assert
        _service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_ShouldSetIsConnectedTrue()
    {
        // Act
        await _service.ConnectAsync();

        // Assert
        _service.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ShouldSetIsConnectedFalse()
    {
        // Arrange
        await _service.ConnectAsync();

        // Act
        await _service.DisconnectAsync();

        // Assert
        _service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_ShouldNotReconnect()
    {
        // Arrange
        await _service.ConnectAsync();

        // Act
        await _service.ConnectAsync();

        // Assert
        _service.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task SendAudioAsync_WhenNotConnected_ShouldNotThrow()
    {
        // Arrange
        var audioData = new byte[] { 0, 1, 2, 3 };

        // Act
        var action = async () => await _service.SendAudioAsync(audioData);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAudioAsync_WhenConnected_ShouldNotThrow()
    {
        // Arrange
        await _service.ConnectAsync();
        var audioData = new byte[] { 0, 1, 2, 3 };

        // Act
        var action = async () => await _service.SendAudioAsync(audioData);

        // Assert
        await action.Should().NotThrowAsync();
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
    public async Task Dispose_WhenConnected_ShouldDisconnect()
    {
        // Arrange
        await _service.ConnectAsync();

        // Act
        _service.Dispose();

        // Assert
        _service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WithoutApiKey_ShouldRaiseErrorEvent()
    {
        // Arrange
        var settingsMock = new Mock<IOptions<DeepgramSettings>>();
        settingsMock.Setup(x => x.Value).Returns(new DeepgramSettings
        {
            ApiKey = "",
            Language = "nl",
            Model = "nova-2"
        });

        var service = new DeepgramTranscriptionService(settingsMock.Object, _loggerMock.Object);
        string? errorMessage = null;
        service.ErrorOccurred += (_, error) => errorMessage = error;

        // Act
        await service.ConnectAsync();

        // Assert
        errorMessage.Should().NotBeNull();
        errorMessage.Should().Contain("API key");
    }
}
