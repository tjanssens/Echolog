using FluentAssertions;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Settings;
using MeetingTranscriber.Services.Summary;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MeetingTranscriber.Tests.Services;

public class ClaudeSummaryServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<ClaudeSummaryService>> _loggerMock;
    private readonly ClaudeSummaryService _service;

    public ClaudeSummaryServiceTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new AppSettings
        {
            ClaudeApiKey = "test-api-key",
            ClaudeModel = "claude-sonnet-4-20250514"
        });

        _loggerMock = new Mock<ILogger<ClaudeSummaryService>>();

        _service = new ClaudeSummaryService(_settingsServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithEmptySegments_ShouldReturnNoTranscriptMessage()
    {
        // Arrange
        var segments = new List<TranscriptSegment>();

        // Act
        var result = await _service.GenerateSummaryAsync(segments);

        // Assert
        result.Should().Contain("Geen transcript");
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithoutApiKey_ShouldReturnConfigurationMessage()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new AppSettings
        {
            ClaudeApiKey = "",
            ClaudeModel = "claude-sonnet-4-20250514"
        });

        var service = new ClaudeSummaryService(settingsServiceMock.Object, _loggerMock.Object);
        var segments = new List<TranscriptSegment>
        {
            new(DateTime.Now, "Speaker 1", "Test text", true, AudioSource.Microphone)
        };

        // Act
        var result = await service.GenerateSummaryAsync(segments);

        // Assert
        result.Should().Contain("API key");
        result.Should().Contain("niet geconfigureerd");
    }

    [Fact]
    public async Task GenerateSummaryAsync_ShouldBeCancellable()
    {
        // Arrange
        var segments = new List<TranscriptSegment>
        {
            new(DateTime.Now, "Speaker 1", "Test", true, AudioSource.Microphone)
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _service.GenerateSummaryAsync(segments, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
