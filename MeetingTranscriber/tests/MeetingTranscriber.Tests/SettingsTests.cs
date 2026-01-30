using FluentAssertions;
using MeetingTranscriber.Services.Settings;
using Xunit;

namespace MeetingTranscriber.Tests;

public class SettingsTests
{
    [Fact]
    public void AppSettings_ShouldHaveDefaultDeepgramValues()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.DeepgramApiKey.Should().BeEmpty();
        settings.DeepgramLanguage.Should().Be("nl");
        settings.DeepgramModel.Should().Be("nova-2");
    }

    [Fact]
    public void AppSettings_ShouldHaveDefaultClaudeValues()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.ClaudeApiKey.Should().BeEmpty();
        settings.ClaudeModel.Should().Be("claude-sonnet-4-20250514");
    }

    [Fact]
    public void AppSettings_ShouldHaveDefaultStorageValues()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.StoragePath.Should().BeEmpty();
    }

    [Fact]
    public void AppSettings_ShouldHaveDefaultAudioValues()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.AudioSampleRate.Should().Be(16000);
        settings.AudioChannels.Should().Be(1);
        settings.AudioBitsPerSample.Should().Be(16);
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(16000)]
    [InlineData(44100)]
    [InlineData(48000)]
    public void AppSettings_ShouldAllowDifferentSampleRates(int sampleRate)
    {
        // Arrange & Act
        var settings = new AppSettings { AudioSampleRate = sampleRate };

        // Assert
        settings.AudioSampleRate.Should().Be(sampleRate);
    }

    [Fact]
    public void AppSettings_ShouldHaveDefaultUIValues()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.AutoScroll.Should().BeTrue();
        settings.DarkTheme.Should().BeTrue();
    }
}
