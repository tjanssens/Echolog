using System.Globalization;
using System.Windows;
using FluentAssertions;
using MeetingTranscriber.Converters;
using Xunit;

namespace MeetingTranscriber.Tests.Converters;

public class ConvertersTests
{
    [Theory]
    [InlineData(true, "Play")]
    [InlineData(false, "Pause")]
    public void BoolToPlayPauseIconConverter_ShouldReturnCorrectIcon(bool isPaused, string expectedIcon)
    {
        // Arrange
        var converter = new BoolToPlayPauseIconConverter();

        // Act
        var result = converter.Convert(isPaused, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(true, "Hervat")]
    [InlineData(false, "Pauze")]
    public void BoolToPauseResumeTextConverter_ShouldReturnCorrectText(bool isPaused, string expectedText)
    {
        // Arrange
        var converter = new BoolToPauseResumeTextConverter();

        // Act
        var result = converter.Convert(isPaused, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expectedText);
    }

    [Theory]
    [InlineData(true, 1.0)]
    [InlineData(false, 0.6)]
    public void BoolToOpacityConverter_ShouldReturnCorrectOpacity(bool isFinal, double expectedOpacity)
    {
        // Arrange
        var converter = new BoolToOpacityConverter();

        // Act
        var result = converter.Convert(isFinal, typeof(double), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expectedOpacity);
    }

    [Theory]
    [InlineData("Some text", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void StringNotEmptyToBoolConverter_ShouldReturnCorrectBool(string? input, bool expected)
    {
        // Arrange
        var converter = new StringNotEmptyToBoolConverter();

        // Act
        var result = converter.Convert(input!, typeof(bool), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Collapsed)]
    public void BooleanToVisibilityConverter_ShouldReturnCorrectVisibility(bool input, Visibility expected)
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.Convert(input, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Collapsed, false)]
    [InlineData(Visibility.Hidden, false)]
    public void BooleanToVisibilityConverter_ConvertBack_ShouldReturnCorrectBool(Visibility input, bool expected)
    {
        // Arrange
        var converter = new BooleanToVisibilityConverter();

        // Act
        var result = converter.ConvertBack(input, typeof(bool), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void BoolToRecordingBrushConverter_WhenRecording_ShouldReturnRedBrush()
    {
        // Arrange
        var converter = new BoolToRecordingBrushConverter();

        // Act
        var result = converter.Convert(true, typeof(object), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void BoolToRecordingBrushConverter_WhenNotRecording_ShouldReturnTransparent()
    {
        // Arrange
        var converter = new BoolToRecordingBrushConverter();

        // Act
        var result = converter.Convert(false, typeof(object), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().NotBeNull();
    }
}
