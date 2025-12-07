namespace gg_file_organizer.Models;

/// <summary>
/// Represents the type of files to process
/// </summary>
public enum FileType
{
    Photo,
    Video,
    All
}

/// <summary>
/// Represents the current operation stage
/// </summary>
public enum OperationStage
{
    Copy,
    Verify,
    Clear,
    CopyMissing,
    DeletePresent
}
