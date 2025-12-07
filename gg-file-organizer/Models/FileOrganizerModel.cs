namespace gg_file_organizer.Models;

/// <summary>
/// Model for the file organizer application
/// </summary>
public class FileOrganizerModel : NotifyPropertyChangedBase
{
    private string? _text;
    private string? _sourcePath;
    private string? _destinationPath;

    public string? Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public string? SourcePath
    {
        get => _sourcePath;
        set => SetProperty(ref _sourcePath, value);
    }

    public string? DestinationPath
    {
        get => _destinationPath;
        set => SetProperty(ref _destinationPath, value);
    }
}
