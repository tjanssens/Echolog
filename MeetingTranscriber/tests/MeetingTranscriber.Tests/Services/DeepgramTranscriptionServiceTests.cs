using FluentAssertions;
using MeetingTranscriber.Services.Settings;
using MeetingTranscriber.Services.Transcription;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MeetingTranscriber.Tests.Services;

public class DeepgramTranscriptionServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<DeepgramTranscriptionService>> _loggerMock;
    private readonly DeepgramTranscriptionService _service;

    public DeepgramTranscriptionServiceTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new AppSettings
        {
            DeepgramApiKey = "test-api-key",
            DeepgramLanguage = "nl",
            DeepgramModel = "nova-2"
        });

        _loggerMock = new Mock<ILogger<DeepgramTranscriptionService>>();

        _service = new DeepgramTranscriptionService(_settingsServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void IsConnected_ShouldBeFalseInitially()
    {
        // Assert
        _service.IsConnected.Should().BeFalse();
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
    public void Dispose_ShouldNotThrow()
    {
        // Act
        var action = () => _service.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task ConnectAsync_WithoutApiKey_ShouldRaiseErrorEvent()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new AppSettings
        {
            DeepgramApiKey = "",
            DeepgramLanguage = "nl",
            DeepgramModel = "nova-2"
        });

        var service = new DeepgramTranscriptionService(settingsServiceMock.Object, _loggerMock.Object);
        string? errorMessage = null;
        service.ErrorOccurred += (_, error) => errorMessage = error;

        // Act
        await service.ConnectAsync();

        // Assert
        errorMessage.Should().NotBeNull();
        errorMessage.Should().Contain("API key");
    }
}
