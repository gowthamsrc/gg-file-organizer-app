using gg_file_organizer.Models;

namespace gg_file_organizer.Services;

/// <summary>
/// Interface for file operations
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Parses date from filename pattern (YYYYMMDD)
    /// </summary>
    DateTime GetDateTimeFromFileName(string path);

    /// <summary>
    /// Checks if a file exists in a directory or its subdirectories
    /// </summary>
    bool FileExistsInDirectory(string fileName, string directory);

    /// <summary>
    /// Gets filtered files based on file type
    /// </summary>
    string[] GetFilteredFiles(string directoryPath, FileType fileType);

    /// <summary>
    /// Copies a file to destination with folder organization
    /// </summary>
    void CopyFile(string sourcePath, string destinationPath, string folderName);

    /// <summary>
    /// Deletes a file
    /// </summary>
    void DeleteFile(string filePath);

    /// <summary>
    /// Saves text content to a file
    /// </summary>
    void SaveTextToFile(string content, string filePath);

    /// <summary>
    /// Validates if paths are valid for operation
    /// </summary>
    bool ValidatePaths(string? sourcePath, string? destinationPath, out string errorMessage);
}
