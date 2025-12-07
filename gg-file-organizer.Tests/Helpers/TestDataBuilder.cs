using System.IO;

namespace gg_file_organizer.Tests.Helpers;

/// <summary>
/// Helper class for creating test data
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a temporary directory for testing
    /// </summary>
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Creates a test file in the specified directory
    /// </summary>
    public static string CreateTestFile(string directory, string fileName, string content = "test content")
    {
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Creates multiple test files with date-based names
    /// </summary>
    public static List<string> CreateDateBasedTestFiles(string directory, int count)
    {
        var files = new List<string>();
        var baseDate = new DateTime(2023, 1, 1);

        for (int i = 0; i < count; i++)
        {
            var date = baseDate.AddDays(i);
            var fileName = $"IMG_{date:yyyyMMdd}_test.jpg";
            files.Add(CreateTestFile(directory, fileName));
        }

        return files;
    }

    /// <summary>
    /// Cleans up a test directory
    /// </summary>
    public static void Cleanup(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
