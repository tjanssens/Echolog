using FluentAssertions;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests.Models;

public class AudioDeviceTests
{
    [Fact]
    public void AudioDevice_ShouldCreateWithCorrectProperties()
    {
        // Arrange & Act
        var device = new AudioDevice("test-id", "Test Microphone", AudioDeviceType.Input);

        // Assert
        device.Id.Should().Be("test-id");
        device.Name.Should().Be("Test Microphone");
        device.Type.Should().Be(AudioDeviceType.Input);
    }

    [Fact]
    public void AudioDevice_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var device1 = new AudioDevice("id-1", "Device", AudioDeviceType.Input);
        var device2 = new AudioDevice("id-1", "Device", AudioDeviceType.Input);

        // Act & Assert
        device1.Should().Be(device2);
    }

    [Fact]
    public void AudioDevice_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var device1 = new AudioDevice("id-1", "Device", AudioDeviceType.Input);
        var device2 = new AudioDevice("id-2", "Device", AudioDeviceType.Input);

        // Act & Assert
        device1.Should().NotBe(device2);
    }

    [Theory]
    [InlineData(AudioDeviceType.Input)]
    [InlineData(AudioDeviceType.Output)]
    public void AudioDeviceType_ShouldHaveExpectedValues(AudioDeviceType type)
    {
        // Assert
        Enum.IsDefined(typeof(AudioDeviceType), type).Should().BeTrue();
    }
}
