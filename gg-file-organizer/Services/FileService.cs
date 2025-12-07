using System.IO;
using System.Text.RegularExpressions;
using gg_file_organizer.Models;

namespace gg_file_organizer.Services;

/// <summary>
/// Implementation of file operations
/// </summary>
public partial class FileService : IFileService
{
    // Compiled regex for better performance
    [GeneratedRegex(@"(\d{4})(\d{2})(\d{2})", RegexOptions.Compiled)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"\.(jpg|jpeg|png)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PhotoExtensionPattern();

    [GeneratedRegex(@"\.(mp4|mkv|avi)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex VideoExtensionPattern();

    public DateTime GetDateTimeFromFileName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return DateTime.MinValue;

        var match = DatePattern().Match(path);
        if (!match.Success)
            return DateTime.MinValue;

        try
        {
            int year = int.Parse(match.Groups[1].Value);
            int month = int.Parse(match.Groups[2].Value);
            int day = int.Parse(match.Groups[3].Value);

            // Validate date components
            if (month < 1 || month > 12 || day < 1 || day > 31)
                return DateTime.MinValue;

            return new DateTime(year, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    public bool FileExistsInDirectory(string fileName, string directory)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(directory))
            return false;

        if (!Directory.Exists(directory))
            return false;

        var fileNameToCheck = Path.Combine(directory, fileName);

        // Check current directory
        if (Directory.GetFiles(directory)
            .Any(x => x.Equals(fileNameToCheck, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check subdirectories recursively
        return Directory.GetDirectories(directory)
            .Any(subDir => FileExistsInDirectory(fileName, subDir));
    }

    public string[] GetFilteredFiles(string directoryPath, FileType fileType)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            return [];

        var files = Directory.GetFiles(directoryPath);

        return fileType switch
        {
            FileType.Photo => files.Where(f => PhotoExtensionPattern().IsMatch(Path.GetExtension(f))).ToArray(),
            FileType.Video => files.Where(f => VideoExtensionPattern().IsMatch(Path.GetExtension(f))).ToArray(),
            FileType.All => files,
            _ => files
        };
    }

    public void CopyFile(string sourcePath, string destinationPath, string folderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderName);

        var destFolderPath = Path.Combine(destinationPath, folderName);
        Directory.CreateDirectory(destFolderPath);

        var fileName = Path.GetFileName(sourcePath);
        var destFilePath = Path.Combine(destFolderPath, fileName);

        File.Copy(sourcePath, destFilePath, overwrite: true);
    }

    public void DeleteFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public void SaveTextToFile(string content, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        File.WriteAllText(filePath, content ?? string.Empty);
    }

    public bool ValidatePaths(string? sourcePath, string? destinationPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            errorMessage = "Source path is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            errorMessage = "Destination path is required.";
            return false;
        }

        if (!Directory.Exists(sourcePath))
        {
            errorMessage = "Source directory does not exist.";
            return false;
        }

        return true;
    }
}
