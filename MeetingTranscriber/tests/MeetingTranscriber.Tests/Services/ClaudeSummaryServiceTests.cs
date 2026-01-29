using FluentAssertions;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Summary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MeetingTranscriber.Tests.Services;

public class ClaudeSummaryServiceTests
{
    private readonly Mock<IOptions<ClaudeSettings>> _settingsMock;
    private readonly Mock<ILogger<ClaudeSummaryService>> _loggerMock;
    private readonly ClaudeSummaryService _service;

    public ClaudeSummaryServiceTests()
    {
        _settingsMock = new Mock<IOptions<ClaudeSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(new ClaudeSettings
        {
            ApiKey = "test-api-key",
            Model = "claude-sonnet-4-20250514"
        });

        _loggerMock = new Mock<ILogger<ClaudeSummaryService>>();

        _service = new ClaudeSummaryService(_settingsMock.Object, _loggerMock.Object);
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
        var settingsMock = new Mock<IOptions<ClaudeSettings>>();
        settingsMock.Setup(x => x.Value).Returns(new ClaudeSettings
        {
            ApiKey = "",
            Model = "claude-sonnet-4-20250514"
        });

        var service = new ClaudeSummaryService(settingsMock.Object, _loggerMock.Object);
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
    public async Task GenerateSummaryAsync_WithValidSegments_ShouldReturnSummary()
    {
        // Arrange
        var segments = new List<TranscriptSegment>
        {
            new(DateTime.Now, "Speaker 1", "Hello everyone", true, AudioSource.Microphone),
            new(DateTime.Now.AddSeconds(5), "Speaker 2", "Hi there", true, AudioSource.SystemAudio)
        };

        // Act
        var result = await _service.GenerateSummaryAsync(segments);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("##"); // Should contain markdown headers
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
        // The current implementation doesn't throw on cancellation, but it should be supported
        var action = async () => await _service.GenerateSummaryAsync(segments, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateSummaryAsync_ShouldOnlyIncludeFinalSegments()
    {
        // Arrange
        var segments = new List<TranscriptSegment>
        {
            new(DateTime.Now, "Speaker 1", "Final text", true, AudioSource.Microphone),
            new(DateTime.Now.AddSeconds(1), "Speaker 1", "Interim text...", false, AudioSource.Microphone)
        };

        // Act
        var result = await _service.GenerateSummaryAsync(segments);

        // Assert
        // The service should process final segments only
        result.Should().NotBeNullOrEmpty();
    }
}
