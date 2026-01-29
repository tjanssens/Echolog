using FluentAssertions;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests.Models;

public class TranscriptSegmentTests
{
    [Fact]
    public void TranscriptSegment_ShouldCreateWithCorrectProperties()
    {
        // Arrange
        var timestamp = DateTime.Now;

        // Act
        var segment = new TranscriptSegment(
            timestamp,
            "Speaker 1",
            "Hello, this is a test.",
            true,
            AudioSource.Microphone
        );

        // Assert
        segment.Timestamp.Should().Be(timestamp);
        segment.Speaker.Should().Be("Speaker 1");
        segment.Text.Should().Be("Hello, this is a test.");
        segment.IsFinal.Should().BeTrue();
        segment.Source.Should().Be(AudioSource.Microphone);
    }

    [Fact]
    public void TranscriptSegment_InterimResult_ShouldHaveIsFinalFalse()
    {
        // Arrange & Act
        var segment = new TranscriptSegment(
            DateTime.Now,
            "Speaker 1",
            "Partial text...",
            false,
            AudioSource.SystemAudio
        );

        // Assert
        segment.IsFinal.Should().BeFalse();
    }

    [Theory]
    [InlineData(AudioSource.Microphone)]
    [InlineData(AudioSource.SystemAudio)]
    public void AudioSource_ShouldHaveExpectedValues(AudioSource source)
    {
        // Assert
        Enum.IsDefined(typeof(AudioSource), source).Should().BeTrue();
    }

    [Fact]
    public void TranscriptSegment_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0);
        var segment1 = new TranscriptSegment(timestamp, "Speaker 1", "Text", true, AudioSource.Microphone);
        var segment2 = new TranscriptSegment(timestamp, "Speaker 1", "Text", true, AudioSource.Microphone);

        // Act & Assert
        segment1.Should().Be(segment2);
    }
}
