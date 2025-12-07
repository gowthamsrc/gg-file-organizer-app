using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using gg_file_organizer.Models;
using gg_file_organizer.Services;
using Microsoft.Win32;

namespace gg_file_organizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IFileService _fileService;
    private readonly BackgroundWorker _backgroundWorker;

    private int _count;
    private readonly List<string> _missingFiles = [];
    private readonly List<string> _presentFiles = [];
    private OperationStage _currentStage = OperationStage.Clear;
    private string _sourceFilePath = string.Empty;
    private string _destinationFilePath = string.Empty;
    private FileType _fileType = FileType.All;
    private bool _isDeleteSelected;

    public MainWindow() : this(new FileService())
    {
    }

    // Constructor for dependency injection (useful for testing)
    public MainWindow(IFileService fileService)
    {
        InitializeComponent();

        _fileService = fileService;
        DataContext = new FileOrganizerModel();

        InitializeUI();

        _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };
        _backgroundWorker.DoWork += BackgroundWorker_DoWork;
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
    }

    private void InitializeUI()
    {
        Output_Window.AppendText("\t\t\t\t\tWelcome to GG file organizer application\n");
        Output_Window.FontSize = 12.00;
        Output_Window.Foreground = Brushes.LawnGreen;
        Common_Progress_Bar.Value = 0;
        Percentage_lbl.Content = "0%";
    }

    #region Mouse Event Handlers

    private void FolderF1_MouseEnter(object sender, MouseEventArgs e)
    {
        SetFolderImage(F1, "Folder_onhover.png");
    }

    private void FolderF1_MouseLeave(object sender, MouseEventArgs e)
    {
        SetFolderImage(F1, "Folder.png");
    }

    private void FolderF2_MouseEnter(object sender, MouseEventArgs e)
    {
        SetFolderImage(F2, "Folder_onhover.png");
    }

    private void FolderF2_MouseLeave(object sender, MouseEventArgs e)
    {
        SetFolderImage(F2, "Folder.png");
    }

    private static void SetFolderImage(System.Windows.Controls.Image imageControl, string imageName)
    {
        var packUri = $@"pack://application:,,,/Resources/{imageName}";
        imageControl.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
    }

    #endregion

    #region Button Click Handlers

    private void Source_open_btn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the path of your source files",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            Source_txt.Text = dialog.FolderName;
            _sourceFilePath = dialog.FolderName;
        }
    }

    private void Destination_open_btn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select the path of your destination files",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            Destination_txt.Text = dialog.FolderName;
            _destinationFilePath = dialog.FolderName;
        }
    }

    private void Copy_btn_Click(object sender, RoutedEventArgs e)
    {
        _isDeleteSelected = false;

        if (!ValidateInputs(out var message))
            return;

        var result = MessageBox.Show(message, "File organizer confirmation window",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            StartOperation(OperationStage.Copy);
        }
    }

    private void Verify_btn_Click(object sender, RoutedEventArgs e)
    {
        _isDeleteSelected = false;

        if (!ValidateInputs(out var message))
            return;

        var result = MessageBox.Show(message, "File organizer confirmation window",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _missingFiles.Clear();
            _presentFiles.Clear();
            StartOperation(OperationStage.Verify);
        }
    }

    private void delete_btn_Click(object sender, RoutedEventArgs e)
    {
        _isDeleteSelected = true;

        if (!ValidateInputs(out var message))
            return;

        var result = MessageBox.Show(message, "File organizer confirmation window",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _missingFiles.Clear();
            _presentFiles.Clear();
            StartOperation(OperationStage.Verify);
        }
    }

    private void Clear_btn_Click(object sender, RoutedEventArgs e)
    {
        _isDeleteSelected = false;
        _sourceFilePath = string.Empty;
        Source_txt.Text = string.Empty;
        _destinationFilePath = string.Empty;
        Destination_txt.Text = string.Empty;
        rb_photo.IsChecked = false;
        rb_video.IsChecked = false;
        rb_all.IsChecked = false;
        Common_Progress_Bar.Value = 0;
        Percentage_lbl.Content = "0%";
        _count = 0;
        _missingFiles.Clear();
        _presentFiles.Clear();
        Output_Window.Document.Blocks.Clear();
        _fileType = FileType.All;
        Output_Window.AppendText("\t\t\t\t\tWelcome to GG file organizer application\n");
    }

    private void Console_Save_btn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save the Console log to File",
            Filter = "Log files (*.log)|*.log"
        };

        if (dialog.ShowDialog() == true)
        {
            var text = new TextRange(Output_Window.Document.ContentStart, Output_Window.Document.ContentEnd).Text;
            _fileService.SaveTextToFile(text, dialog.FileName);
        }
    }

    #endregion

    #region Helper Methods

    private bool ValidateInputs(out string confirmationMessage)
    {
        confirmationMessage = string.Empty;

        if (rb_photo.IsChecked != true && rb_video.IsChecked != true && rb_all.IsChecked != true)
        {
            MessageBox.Show("Please choose any one file type and retry!", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!_fileService.ValidatePaths(Source_txt.Text, Destination_txt.Text, out var errorMessage))
        {
            MessageBox.Show(errorMessage, "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (rb_photo.IsChecked == true)
        {
            confirmationMessage = "You have chosen photo organizer. Do you want to proceed?";
            _fileType = FileType.Photo;
        }
        else if (rb_video.IsChecked == true)
        {
            confirmationMessage = "You have chosen video organizer. Do you want to proceed?";
            _fileType = FileType.Video;
        }
        else
        {
            confirmationMessage = "You have chosen file organizer. Do you want to proceed?";
            _fileType = FileType.All;
        }

        return true;
    }

    private void StartOperation(OperationStage stage)
    {
        _count = 0;
        SetButtonsEnabled(false);

        _sourceFilePath = Source_txt.Text;
        _destinationFilePath = Destination_txt.Text;

        Output_Window.Document.Blocks.Clear();
        Output_Window.AppendText("\t\t\t\t\tWelcome to GG file organizer application\n");
        Output_Window.AppendText($"SOURCE FOLDER PATH:\t{_sourceFilePath}\n");
        Output_Window.AppendText($"DESTINATION FOLDER PATH:\t{_destinationFilePath}\n");
        Output_Window.ScrollToEnd();

        _currentStage = stage;
        _backgroundWorker.RunWorkerAsync();
    }

    private void SetButtonsEnabled(bool enabled)
    {
        verify_btn.IsEnabled = enabled;
        copy_btn.IsEnabled = enabled;
        clear_btn.IsEnabled = enabled;
        delete_btn.IsEnabled = enabled;
    }

    #endregion

    #region Background Worker

    private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        Thread.Sleep(500);

        switch (_currentStage)
        {
            case OperationStage.Verify:
                ProcessVerify();
                break;
            case OperationStage.Copy:
                ProcessCopy();
                break;
            case OperationStage.CopyMissing:
                ProcessCopyMissing();
                break;
            case OperationStage.DeletePresent:
                ProcessDeletePresent();
                break;
        }
    }

    private void ProcessVerify()
    {
        var sourceFiles = _fileService.GetFilteredFiles(_sourceFilePath, _fileType);

        for (int i = 0; i < sourceFiles.Length; i++)
        {
            var fileName = Path.GetFileName(sourceFiles[i]);
            var percentage = (i + 1) * 100 / sourceFiles.Length;

            if (!_fileService.FileExistsInDirectory(fileName, _destinationFilePath))
            {
                _missingFiles.Add(sourceFiles[i]);
                _backgroundWorker.ReportProgress(percentage, $"{_count}) File Name: {sourceFiles[i]} not found!!!");
            }
            else
            {
                _presentFiles.Add(sourceFiles[i]);
                _backgroundWorker.ReportProgress(percentage, $"{_count}) File Name: {sourceFiles[i]} found!!!");
            }
            _count++;
            Thread.Sleep(50);
        }
    }

    private void ProcessCopy()
    {
        var sourceFiles = _fileService.GetFilteredFiles(_sourceFilePath, _fileType);

        for (int i = 0; i < sourceFiles.Length; i++)
        {
            var fileName = Path.GetFileName(sourceFiles[i]);
            var fileDate = _fileService.GetDateTimeFromFileName(sourceFiles[i]);
            var folderName = fileDate.ToString("yyyy-MM-dd");
            var percentage = (i + 1) * 100 / sourceFiles.Length;

            if (!_fileService.FileExistsInDirectory(fileName, _destinationFilePath))
            {
                _fileService.CopyFile(sourceFiles[i], _destinationFilePath, folderName);
                _count++;
                _backgroundWorker.ReportProgress(percentage, $"{_count}) File Name: {sourceFiles[i]}");
            }
            _backgroundWorker.ReportProgress(percentage);
            Thread.Sleep(50);
        }
    }

    private void ProcessCopyMissing()
    {
        for (int i = 0; i < _missingFiles.Count; i++)
        {
            var fileName = Path.GetFileName(_missingFiles[i]);
            var fileDate = _fileService.GetDateTimeFromFileName(_missingFiles[i]);
            var folderName = fileDate.ToString("yyyy-MM-dd");
            var percentage = (i + 1) * 100 / _missingFiles.Count;

            if (!_fileService.FileExistsInDirectory(fileName, _destinationFilePath))
            {
                _fileService.CopyFile(_missingFiles[i], _destinationFilePath, folderName);
                _count++;
                _backgroundWorker.ReportProgress(percentage, $"{_count}) File Name: {_missingFiles[i]}");
            }
            _backgroundWorker.ReportProgress(percentage);
            Thread.Sleep(50);
        }
    }

    private void ProcessDeletePresent()
    {
        for (int i = 0; i < _presentFiles.Count; i++)
        {
            var fileName = Path.GetFileName(_presentFiles[i]);
            var percentage = (i + 1) * 100 / _presentFiles.Count;

            if (_fileService.FileExistsInDirectory(fileName, _destinationFilePath))
            {
                _fileService.DeleteFile(_presentFiles[i]);
                _count++;
                _backgroundWorker.ReportProgress(percentage, $"{_count}) File Name: {_presentFiles[i]}");
            }
            _backgroundWorker.ReportProgress(percentage);
            Thread.Sleep(50);
        }
    }

    private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        Common_Progress_Bar.Value = e.ProgressPercentage;
        Percentage_lbl.Content = $"{e.ProgressPercentage}%";

        if (e.UserState is string message && !string.IsNullOrEmpty(message))
        {
            Output_Window.AppendText($"\n{message}");
            Output_Window.ScrollToEnd();
        }
    }

    private void BackgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        if (_currentStage == OperationStage.Verify)
        {
            HandleVerifyCompleted();
        }
        else
        {
            Output_Window.AppendText($"\nTOTAL NUMBER OF FILES PROCESSED: {_count}\n");
            Output_Window.ScrollToEnd();
            SetButtonsEnabled(true);
        }
    }

    private void HandleVerifyCompleted()
    {
        Output_Window.AppendText($"\nTOTAL NUMBER OF FILES NOT PRESENT: {_missingFiles.Count}\n");
        Output_Window.ScrollToEnd();
        SetButtonsEnabled(true);

        if (!_isDeleteSelected && _missingFiles.Count > 0)
        {
            var result = MessageBox.Show("Do you want to copy the Missing Files?", "Copy File Window",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Output_Window.AppendText("\nStarting to copy missing files :)\n");
                StartOperation(OperationStage.CopyMissing);
            }
        }
        else if (_isDeleteSelected && _presentFiles.Count > 0)
        {
            var result = MessageBox.Show("Do you want to delete the files which are present?", "Delete File Window",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Output_Window.AppendText("\nStarting to delete present files :)\n");
                StartOperation(OperationStage.DeletePresent);
            }
        }
    }

    #endregion
}
