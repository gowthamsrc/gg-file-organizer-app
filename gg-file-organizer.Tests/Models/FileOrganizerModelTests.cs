using FluentAssertions;
using gg_file_organizer.Models;
using Xunit;

namespace gg_file_organizer.Tests.Models;

public class FileOrganizerModelTests
{
    [Fact]
    public void SetProperty_ValueChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var model = new FileOrganizerModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        model.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = args.PropertyName;
        };

        // Act
        model.Text = "New Value";

        // Assert
        propertyChangedRaised.Should().BeTrue();
        changedPropertyName.Should().Be(nameof(FileOrganizerModel.Text));
    }

    [Fact]
    public void SetProperty_SameValue_DoesNotRaisePropertyChangedEvent()
    {
        // Arrange
        var model = new FileOrganizerModel { Text = "Same Value" };
        var propertyChangedRaised = false;

        model.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
        };

        // Act
        model.Text = "Same Value";

        // Assert
        propertyChangedRaised.Should().BeFalse();
    }

    [Fact]
    public void SourcePath_SetValue_UpdatesProperty()
    {
        // Arrange
        var model = new FileOrganizerModel();

        // Act
        model.SourcePath = "C:\\Source";

        // Assert
        model.SourcePath.Should().Be("C:\\Source");
    }

    [Fact]
    public void DestinationPath_SetValue_UpdatesProperty()
    {
        // Arrange
        var model = new FileOrganizerModel();

        // Act
        model.DestinationPath = "C:\\Destination";

        // Assert
        model.DestinationPath.Should().Be("C:\\Destination");
    }

    [Fact]
    public void SourcePath_SetValue_RaisesPropertyChangedEvent()
    {
        // Arrange
        var model = new FileOrganizerModel();
        string? changedPropertyName = null;

        model.PropertyChanged += (sender, args) =>
        {
            changedPropertyName = args.PropertyName;
        };

        // Act
        model.SourcePath = "C:\\NewSource";

        // Assert
        changedPropertyName.Should().Be(nameof(FileOrganizerModel.SourcePath));
    }

    [Fact]
    public void DestinationPath_SetValue_RaisesPropertyChangedEvent()
    {
        // Arrange
        var model = new FileOrganizerModel();
        string? changedPropertyName = null;

        model.PropertyChanged += (sender, args) =>
        {
            changedPropertyName = args.PropertyName;
        };

        // Act
        model.DestinationPath = "C:\\NewDestination";

        // Assert
        changedPropertyName.Should().Be(nameof(FileOrganizerModel.DestinationPath));
    }
}
