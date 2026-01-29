using FluentAssertions;
using Xunit;

namespace MeetingTranscriber.Tests;

public class SettingsTests
{
    [Fact]
    public void DeepgramSettings_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var settings = new DeepgramSettings();

        // Assert
        settings.ApiKey.Should().BeEmpty();
        settings.Language.Should().Be("nl");
        settings.Model.Should().Be("nova-2");
    }

    [Fact]
    public void ClaudeSettings_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var settings = new ClaudeSettings();

        // Assert
        settings.ApiKey.Should().BeEmpty();
        settings.Model.Should().Be("claude-sonnet-4-20250514");
    }

    [Fact]
    public void StorageSettings_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var settings = new StorageSettings();

        // Assert
        settings.BasePath.Should().Be("%APPDATA%/MeetingTranscriber");
    }

    [Fact]
    public void StorageSettings_GetExpandedPath_ShouldExpandEnvironmentVariables()
    {
        // Arrange
        var settings = new StorageSettings
        {
            BasePath = "%TEMP%/TestPath"
        };

        // Act
        var expandedPath = settings.GetExpandedPath();

        // Assert
        expandedPath.Should().NotContain("%TEMP%");
        expandedPath.Should().EndWith("TestPath");
    }

    [Fact]
    public void StorageSettings_GetExpandedPath_WithNoVariables_ShouldReturnSamePath()
    {
        // Arrange
        var settings = new StorageSettings
        {
            BasePath = "/absolute/path/to/storage"
        };

        // Act
        var expandedPath = settings.GetExpandedPath();

        // Assert
        expandedPath.Should().Be("/absolute/path/to/storage");
    }

    [Fact]
    public void AudioSettings_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var settings = new AudioSettings();

        // Assert
        settings.SampleRate.Should().Be(16000);
        settings.Channels.Should().Be(1);
        settings.BitsPerSample.Should().Be(16);
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(16000)]
    [InlineData(44100)]
    [InlineData(48000)]
    public void AudioSettings_ShouldAllowDifferentSampleRates(int sampleRate)
    {
        // Arrange & Act
        var settings = new AudioSettings { SampleRate = sampleRate };

        // Assert
        settings.SampleRate.Should().Be(sampleRate);
    }
}
