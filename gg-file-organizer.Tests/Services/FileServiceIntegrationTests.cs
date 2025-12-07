using System.IO;
using FluentAssertions;
using gg_file_organizer.Models;
using gg_file_organizer.Services;
using Xunit;

namespace gg_file_organizer.Tests.Services;

[Trait("Category", "Integration")]
public class FileServiceIntegrationTests : IDisposable
{
    private readonly FileService _sut;
    private readonly string _tempDirectory;

    public FileServiceIntegrationTests()
    {
        _sut = new FileService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"FileServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    #region FileExistsInDirectory Tests

    [Fact]
    public void FileExistsInDirectory_FileExistsInRoot_ReturnsTrue()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "test.jpg");
        File.WriteAllText(testFile, "test content");

        // Act
        var result = _sut.FileExistsInDirectory("test.jpg", _tempDirectory);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExistsInDirectory_FileExistsInSubdirectory_ReturnsTrue()
    {
        // Arrange
        var subDir = Path.Combine(_tempDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var testFile = Path.Combine(subDir, "test.jpg");
        File.WriteAllText(testFile, "test content");

        // Act
        var result = _sut.FileExistsInDirectory("test.jpg", _tempDirectory);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExistsInDirectory_FileDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = _sut.FileExistsInDirectory("nonexistent.jpg", _tempDirectory);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetFilteredFiles Tests

    [Fact]
    public void GetFilteredFiles_PhotoType_ReturnsOnlyPhotoFiles()
    {
        // Arrange
        CreateTestFile("photo1.jpg");
        CreateTestFile("photo2.png");
        CreateTestFile("photo3.jpeg");
        CreateTestFile("video.mp4");
        CreateTestFile("document.pdf");

        // Act
        var result = _sut.GetFilteredFiles(_tempDirectory, FileType.Photo);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(f =>
            Path.GetExtension(f).ToLower().Should().BeOneOf(".jpg", ".jpeg", ".png"));
    }

    [Fact]
    public void GetFilteredFiles_VideoType_ReturnsOnlyVideoFiles()
    {
        // Arrange
        CreateTestFile("video1.mp4");
        CreateTestFile("video2.mkv");
        CreateTestFile("video3.avi");
        CreateTestFile("photo.jpg");
        CreateTestFile("document.pdf");

        // Act
        var result = _sut.GetFilteredFiles(_tempDirectory, FileType.Video);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(f =>
            Path.GetExtension(f).ToLower().Should().BeOneOf(".mp4", ".mkv", ".avi"));
    }

    [Fact]
    public void GetFilteredFiles_AllType_ReturnsAllFiles()
    {
        // Arrange
        CreateTestFile("photo.jpg");
        CreateTestFile("video.mp4");
        CreateTestFile("document.pdf");

        // Act
        var result = _sut.GetFilteredFiles(_tempDirectory, FileType.All);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region CopyFile Tests

    [Fact]
    public void CopyFile_ValidParameters_CopiesFileToDestination()
    {
        // Arrange
        var sourceFile = CreateTestFile("IMG_20231225_test.jpg");
        var destDir = Path.Combine(_tempDirectory, "destination");

        // Act
        _sut.CopyFile(sourceFile, destDir, "2023-12-25");

        // Assert
        var expectedPath = Path.Combine(destDir, "2023-12-25", "IMG_20231225_test.jpg");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public void CopyFile_DestinationFolderNotExists_CreatesFolderAndCopies()
    {
        // Arrange
        var sourceFile = CreateTestFile("test.jpg");
        var destDir = Path.Combine(_tempDirectory, "new_destination");
        var folderName = "2023-12-25";

        // Act
        _sut.CopyFile(sourceFile, destDir, folderName);

        // Assert
        Directory.Exists(Path.Combine(destDir, folderName)).Should().BeTrue();
    }

    [Fact]
    public void CopyFile_EmptySourcePath_ThrowsArgumentException()
    {
        // Act
        var action = () => _sut.CopyFile("", _tempDirectory, "folder");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region DeleteFile Tests

    [Fact]
    public void DeleteFile_FileExists_DeletesFile()
    {
        // Arrange
        var testFile = CreateTestFile("to_delete.jpg");

        // Act
        _sut.DeleteFile(testFile);

        // Assert
        File.Exists(testFile).Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_FileNotExists_DoesNotThrow()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.jpg");

        // Act
        var action = () => _sut.DeleteFile(nonExistentFile);

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region SaveTextToFile Tests

    [Fact]
    public void SaveTextToFile_ValidParameters_SavesContent()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "output.log");
        var content = "Test log content";

        // Act
        _sut.SaveTextToFile(content, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be(content);
    }

    #endregion

    #region ValidatePaths Tests

    [Fact]
    public void ValidatePaths_ValidPaths_ReturnsTrue()
    {
        // Act
        var result = _sut.ValidatePaths(_tempDirectory, _tempDirectory, out var errorMessage);

        // Assert
        result.Should().BeTrue();
        errorMessage.Should().BeEmpty();
    }

    #endregion

    private string CreateTestFile(string fileName)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(filePath, "test content");
        return filePath;
    }
}
