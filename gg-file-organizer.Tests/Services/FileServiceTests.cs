using FluentAssertions;
using gg_file_organizer.Models;
using gg_file_organizer.Services;
using Xunit;

namespace gg_file_organizer.Tests.Services;

public class FileServiceTests
{
    private readonly FileService _sut;

    public FileServiceTests()
    {
        _sut = new FileService();
    }

    #region GetDateTimeFromFileName Tests

    [Theory]
    [InlineData("IMG_20231225_123456.jpg", 2023, 12, 25)]
    [InlineData("VID_20240101_000000.mp4", 2024, 1, 1)]
    [InlineData("photo_20230615_test.png", 2023, 6, 15)]
    [InlineData("20220314_photo.jpg", 2022, 3, 14)]
    [InlineData("C:\\Photos\\IMG_20231225_test.jpg", 2023, 12, 25)]
    public void GetDateTimeFromFileName_ValidDateInFilename_ReturnsCorrectDate(
        string filename, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Act
        var result = _sut.GetDateTimeFromFileName(filename);

        // Assert
        result.Year.Should().Be(expectedYear);
        result.Month.Should().Be(expectedMonth);
        result.Day.Should().Be(expectedDay);
    }

    [Theory]
    [InlineData("random_file.jpg")]
    [InlineData("no_date_here.mp4")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetDateTimeFromFileName_InvalidFilename_ReturnsMinValue(string filename)
    {
        // Act
        var result = _sut.GetDateTimeFromFileName(filename);

        // Assert
        result.Should().Be(DateTime.MinValue);
    }

    [Theory]
    [InlineData("IMG_20231325_test.jpg")] // Invalid month (13)
    [InlineData("IMG_20230032_test.jpg")] // Invalid day (32)
    [InlineData("IMG_20230000_test.jpg")] // Invalid month and day (0)
    public void GetDateTimeFromFileName_InvalidDateComponents_ReturnsMinValue(string filename)
    {
        // Act
        var result = _sut.GetDateTimeFromFileName(filename);

        // Assert
        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void GetDateTimeFromFileName_NullFilename_ReturnsMinValue()
    {
        // Act
        var result = _sut.GetDateTimeFromFileName(null!);

        // Assert
        result.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region GetFilteredFiles Tests

    [Fact]
    public void GetFilteredFiles_EmptyPath_ReturnsEmptyArray()
    {
        // Act
        var result = _sut.GetFilteredFiles("", FileType.All);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetFilteredFiles_NonExistentDirectory_ReturnsEmptyArray()
    {
        // Act
        var result = _sut.GetFilteredFiles("C:\\NonExistent\\Path\\That\\Does\\Not\\Exist", FileType.All);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ValidatePaths Tests

    [Fact]
    public void ValidatePaths_NullSourcePath_ReturnsFalseWithErrorMessage()
    {
        // Act
        var result = _sut.ValidatePaths(null, "C:\\Destination", out var errorMessage);

        // Assert
        result.Should().BeFalse();
        errorMessage.Should().Be("Source path is required.");
    }

    [Fact]
    public void ValidatePaths_NullDestinationPath_ReturnsFalseWithErrorMessage()
    {
        // Act
        var result = _sut.ValidatePaths("C:\\Source", null, out var errorMessage);

        // Assert
        result.Should().BeFalse();
        errorMessage.Should().Be("Destination path is required.");
    }

    [Fact]
    public void ValidatePaths_EmptySourcePath_ReturnsFalseWithErrorMessage()
    {
        // Act
        var result = _sut.ValidatePaths("   ", "C:\\Destination", out var errorMessage);

        // Assert
        result.Should().BeFalse();
        errorMessage.Should().Be("Source path is required.");
    }

    [Fact]
    public void ValidatePaths_NonExistentSourceDirectory_ReturnsFalseWithErrorMessage()
    {
        // Act
        var result = _sut.ValidatePaths("C:\\NonExistent\\Source\\Path", "C:\\Destination", out var errorMessage);

        // Assert
        result.Should().BeFalse();
        errorMessage.Should().Be("Source directory does not exist.");
    }

    #endregion

    #region FileExistsInDirectory Tests

    [Fact]
    public void FileExistsInDirectory_EmptyFileName_ReturnsFalse()
    {
        // Act
        var result = _sut.FileExistsInDirectory("", "C:\\SomeDirectory");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExistsInDirectory_EmptyDirectory_ReturnsFalse()
    {
        // Act
        var result = _sut.FileExistsInDirectory("test.jpg", "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExistsInDirectory_NonExistentDirectory_ReturnsFalse()
    {
        // Act
        var result = _sut.FileExistsInDirectory("test.jpg", "C:\\NonExistent\\Path");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
