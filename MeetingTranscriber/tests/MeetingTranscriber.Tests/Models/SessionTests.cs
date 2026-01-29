using FluentAssertions;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests.Models;

public class SessionTests
{
    [Fact]
    public void Session_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var session = new Session();

        // Assert
        session.Id.Should().Be(Guid.Empty);
        session.Title.Should().BeEmpty();
        session.AudioInputPath.Should().BeEmpty();
        session.AudioOutputPath.Should().BeEmpty();
        session.AudioMixedPath.Should().BeEmpty();
        session.Segments.Should().NotBeNull().And.BeEmpty();
        session.Summary.Should().BeNull();
        session.SpeakerLabels.Should().NotBeNull().And.BeEmpty();
        session.EndTime.Should().BeNull();
    }

    [Fact]
    public void Session_ShouldAllowSettingProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startTime = DateTime.Now;
        var endTime = startTime.AddHours(1);

        // Act
        var session = new Session
        {
            Id = id,
            StartTime = startTime,
            EndTime = endTime,
            Title = "Test Meeting",
            AudioInputPath = "/path/to/input.wav",
            AudioOutputPath = "/path/to/output.wav",
            AudioMixedPath = "/path/to/mixed.wav",
            Summary = "Meeting summary"
        };

        // Assert
        session.Id.Should().Be(id);
        session.StartTime.Should().Be(startTime);
        session.EndTime.Should().Be(endTime);
        session.Title.Should().Be("Test Meeting");
        session.AudioInputPath.Should().Be("/path/to/input.wav");
        session.AudioOutputPath.Should().Be("/path/to/output.wav");
        session.AudioMixedPath.Should().Be("/path/to/mixed.wav");
        session.Summary.Should().Be("Meeting summary");
    }

    [Fact]
    public void Session_ShouldAllowAddingSegments()
    {
        // Arrange
        var session = new Session();
        var segment = new TranscriptSegment(
            DateTime.Now,
            "Speaker 1",
            "Hello",
            true,
            AudioSource.Microphone
        );

        // Act
        session.Segments.Add(segment);

        // Assert
        session.Segments.Should().HaveCount(1);
        session.Segments[0].Should().Be(segment);
    }

    [Fact]
    public void Session_ShouldAllowAddingSpeakerLabels()
    {
        // Arrange
        var session = new Session();

        // Act
        session.SpeakerLabels["Speaker 1"] = "John Doe";
        session.SpeakerLabels["Speaker 2"] = "Jane Smith";

        // Assert
        session.SpeakerLabels.Should().HaveCount(2);
        session.SpeakerLabels["Speaker 1"].Should().Be("John Doe");
        session.SpeakerLabels["Speaker 2"].Should().Be("Jane Smith");
    }

    [Fact]
    public void Session_Duration_ShouldBeCalculatable()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 15, 10, 0, 0);
        var endTime = new DateTime(2024, 1, 15, 11, 30, 0);

        var session = new Session
        {
            StartTime = startTime,
            EndTime = endTime
        };

        // Act
        var duration = session.EndTime - session.StartTime;

        // Assert
        duration.Should().Be(TimeSpan.FromMinutes(90));
    }
}
