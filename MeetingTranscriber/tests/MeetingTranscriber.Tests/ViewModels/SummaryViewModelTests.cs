using FluentAssertions;
using MeetingTranscriber.ViewModels;
using Xunit;

namespace MeetingTranscriber.Tests.ViewModels;

public class SummaryViewModelTests
{
    [Fact]
    public void SummaryText_ShouldBeEmptyInitially()
    {
        // Arrange & Act
        var viewModel = new SummaryViewModel();

        // Assert
        viewModel.SummaryText.Should().BeEmpty();
    }

    [Fact]
    public void IsGenerating_ShouldBeFalseInitially()
    {
        // Arrange & Act
        var viewModel = new SummaryViewModel();

        // Assert
        viewModel.IsGenerating.Should().BeFalse();
    }

    [Fact]
    public void SummaryText_ShouldRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new SummaryViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SummaryViewModel.SummaryText))
                propertyChanged = true;
        };

        // Act
        viewModel.SummaryText = "New summary";

        // Assert
        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldResetSummaryText()
    {
        // Arrange
        var viewModel = new SummaryViewModel
        {
            SummaryText = "Some summary text"
        };

        // Act
        viewModel.Clear();

        // Assert
        viewModel.SummaryText.Should().BeEmpty();
    }
}
