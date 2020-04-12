﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.Forms.MessageBox;
using MessageBoxImage = System.Windows.Forms.MessageBoxIcon;
using MessageBoxButton = System.Windows.Forms.MessageBoxButtons;
using MessageBoxResult = System.Windows.Forms.DialogResult;

namespace gg_file_organizer
{
    public class Model : INotifyPropertyChanged
    {
        string _text;
        public string Text { get { return _text; } set { _text = value; OnPropertyChanged("Text"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(
                    this,
                    new PropertyChangedEventArgs(propertyName)
                    );
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new Model();
            Output_Window.AppendText("\t\t\t\t\tWelcome to GG file organizer application\n");
            Output_Window.FontSize = 12.00;
            Output_Window.Foreground = Brushes.LawnGreen;
            Common_Progress_Bar.Value = 0;
            Percentage_lbl.Content = "0%";
            this.BGworker_FileApp.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BGworker_FileApp_DoWork);
            this.BGworker_FileApp.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BGworker_FileApp_ProgressChanged);
            this.BGworker_FileApp.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BGworker_FileApp_RunWorkerCompleted);
            BGworker_FileApp.WorkerReportsProgress = true;
        }

        private void FolderF1_MouseEnter(object sender, MouseEventArgs e)
        {
            string packUri = @"pack://application:,,,/Resources/Folder_onhover.png";
            F1.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
        }

        private void FolderF1_MouseLeave(object sender, MouseEventArgs e)
        {
            string packUri = @"pack://application:,,,/Resources/Folder.png";
            F1.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
        }

        private void FolderF2_MouseEnter(object sender, MouseEventArgs e)
        {
            string packUri = @"pack://application:,,,/Resources/Folder_onhover.png";
            F2.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
        }

        private void FolderF2_MouseLeave(object sender, MouseEventArgs e)
        {
            string packUri = @"pack://application:,,,/Resources/Folder.png";
            F2.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
        }

        #region Enum

        public enum stage
        {
            [Description("COPY")]
            COPY,
            [Description("VERIFY")]
            VERIFY,
            [Description("CLEAR")]
            CLEAR,
            [Description("COPYMISSING")]
            COPYMISSING
        }
        public enum FileType
        {
            [Description("Photo")]
            Photo,
            [Description("Video")]
            Video,
            [Description("All")]
            All
        }
        #endregion

        #region Variable and Propertie Declarations
        private int count = 0;
        private List<string> missingfiles = new List<string>();
        private stage current_stage = stage.CLEAR;
        private string SourceFilePath = string.Empty;
        private string DestinationFilePath = string.Empty;
        private FileType fileType = FileType.All;
        #endregion

        #region Public Methods
        private bool FilePresent(string fileName, string directory)
        {
            var exists = false;
            var fileNameToCheck = System.IO.Path.Combine(directory, fileName);
            if (Directory.Exists(directory))
            {
                //check directory for file
                exists = Directory.GetFiles(directory).Any(x => x.Equals(fileNameToCheck, StringComparison.OrdinalIgnoreCase));

                //check subdirectories for file
                if (!exists)
                {
                    foreach (var dir in Directory.GetDirectories(directory))
                    {
                        exists = FilePresent(fileName, dir);

                        if (exists) break;
                    }
                }
            }
            return exists;
        }

        //retrieves the datetime WITHOUT loading the whole image
        public static DateTime GetDateTakenFromImage(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (System.Drawing.Image myImage = System.Drawing.Image.FromStream(fs, false, false))
            {
                if (myImage.PropertyIdList.Any(x => x == 36867))
                {
                    PropertyItem propItem = myImage.GetPropertyItem(36867);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                    return DateTime.Parse(dateTaken);
                }
                else if (myImage.PropertyIdList.Any(x => x == 36868))
                {
                    PropertyItem propItem = myImage.GetPropertyItem(36868);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                    return DateTime.Parse(dateTaken);
                }
                else
                    return DateTime.Parse(DateTime.Now.ToString());

            }
        }

        public static DateTime GetDateTimeFromFile(string path, FileType ft)
        {

            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            var oShell = Activator.CreateInstance(shellAppType);
            var folder = (Shell32.Folder)shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, oShell, new object[] { System.IO.Path.GetDirectoryName(path) });
            var file = (Shell32.FolderItem)shellAppType.InvokeMember("ParseName", System.Reflection.BindingFlags.InvokeMethod, null, folder, new object[] { System.IO.Path.GetFileName(path) });
            //Shell32.Shell shell = new Shell32.Shell();
            //Shell32.Folder folder = shell.NameSpace(System.IO.Path.GetDirectoryName(path));
            //Shell32.FolderItem file = folder.ParseName(System.IO.Path.GetFileName(path));

            // These are the characters that are not allowing me to parse into a DateTime
            char[] charactersToRemove = new char[] {
                (char)8206,
                (char)8207
            };

            int property_id = 0;
            if (ft == FileType.Photo)
            {
                property_id = 12;
            }
            else if (ft == FileType.Video)
            {
                property_id = 208;
            }
            else
            {
                property_id = 3;
            }

            // Getting the "Media Created" label (don't really need this, but what the heck)
            string name = folder.GetDetailsOf(null, property_id);

            // Getting the "Media Created" value as a string
            string value = folder.GetDetailsOf(file, property_id).Trim();

            // Removing the suspect characters
            foreach (char c in charactersToRemove)
                value = value.Replace((c).ToString(), "").Trim();

            // If the value string is empty, return DateTime.MinValue, otherwise return the "Media Created" date
            return value == string.Empty ? DateTime.MinValue : DateTime.Parse(value);
            //return File.GetLastWriteTime(path);
        }

        private void CopyPictures(string source, string destination, string foldername)
        {
            FileInfo fs = new FileInfo(source);
            if (!Directory.Exists(System.IO.Path.Combine(destination, foldername)))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(destination, foldername));
            }
            File.Copy(source, System.IO.Path.Combine(destination, foldername, fs.Name), true);
        }

        private void SaveData(string data, string path)
        {
            //Use StreamWriter class.
            using (StreamWriter sw = new StreamWriter(path))
            {
                //Use write method to write the text
                sw.Write(data);
                //always close your stream
                sw.Close();
            }
        }

        #endregion

        //we init this once so that if the function is repeatedly called
        //it isn't stressing the garbage man
        private static Regex r = new Regex(":");

        #region BackgroundWorker
        BackgroundWorker BGworker_FileApp = new BackgroundWorker();
        private void BGworker_FileApp_DoWork(object sender, DoWorkEventArgs e)
        {
            if(current_stage == stage.VERIFY)
            {
                System.Threading.Thread.Sleep(500);
                string[] sourcefilenames = null;
                if (fileType == FileType.Photo)
                    sourcefilenames = Directory.GetFiles(SourceFilePath).Where(file => Regex.IsMatch(System.IO.Path.GetExtension(file).ToLower(), @"\.(jpg|jpeg|png)")).ToArray();
                else if (fileType == FileType.Video)
                    sourcefilenames = Directory.GetFiles(SourceFilePath).Where(file => Regex.IsMatch(System.IO.Path.GetExtension(file).ToLower(), @"\.(mp4|mkv|avi)")).ToArray();
                else
                    sourcefilenames = Directory.GetFiles(SourceFilePath);

                int i = 0;

                foreach (var f in sourcefilenames)
                {
                    FileInfo Fs = new FileInfo(f);
                    //DateTime FileDate = Fs.LastWriteTime;
                    string filename = Fs.Name;
                    int percentage = (i + 1) * 100 / sourcefilenames.Count();
                    BGworker_FileApp.ReportProgress(percentage);
                    if (!FilePresent(filename, DestinationFilePath))
                    {
                        missingfiles.Add(Fs.FullName);
                        BGworker_FileApp.ReportProgress(percentage, string.Format("{0}) File Name: {1} not found!!!", count, Fs.FullName));
                        count++;
                    }
                    System.Threading.Thread.Sleep(50);
                    i++;
                }
            }
            else if(current_stage == stage.COPY)
            {
                System.Threading.Thread.Sleep(500);

                string[] sourcefilenames = null;
                if (fileType == FileType.Photo)
                    sourcefilenames = Directory.GetFiles(SourceFilePath).Where(file => Regex.IsMatch(System.IO.Path.GetExtension(file).ToLower(), @"\.(jpg|jpeg|png)")).ToArray();
                else if (fileType == FileType.Video)
                    sourcefilenames = Directory.GetFiles(SourceFilePath).Where(file => Regex.IsMatch(System.IO.Path.GetExtension(file).ToLower(), @"\.(mp4|mkv|avi)")).ToArray();
                else
                    sourcefilenames = Directory.GetFiles(SourceFilePath);

                int i = 0;
                foreach (var f in sourcefilenames)
                {
                    FileInfo Fs = new FileInfo(f);
                    //DateTime FileDate = Fs.LastWriteTime;
                    DateTime FileDate;
                    if (fileType == FileType.Photo)
                    {
                        FileDate = GetDateTimeFromFile(f, FileType.Photo);
                    }
                    else if (fileType == FileType.Video)
                        FileDate = GetDateTimeFromFile(f, FileType.Video);
                    else
                        FileDate = GetDateTimeFromFile(f, FileType.All);

                    string foldername = FileDate.ToString("yyyy-MM-dd");
                    string filename = Fs.Name;
                    int percentage = (i + 1) * 100 / sourcefilenames.Count();
                    if (!FilePresent(filename, DestinationFilePath))
                    {
                        CopyPictures(f, DestinationFilePath, foldername);
                        count++;
                        BGworker_FileApp.ReportProgress(percentage, string.Format("{1}) File Name: {0}", Fs.FullName, count));
                    }
                    BGworker_FileApp.ReportProgress(percentage);
                    System.Threading.Thread.Sleep(50);
                    i++;
                }
            }
            else if(current_stage == stage.COPYMISSING)
            {
                System.Threading.Thread.Sleep(500);
                int i = 0;
                foreach (var f in missingfiles)
                {
                    FileInfo Fs = new FileInfo(f);
                    //DateTime FileDate = Fs.LastWriteTime;
                    //DateTime FileDate = GetDateTakenFromImage(f);
                    DateTime FileDate;
                    if (fileType == FileType.Photo)
                        FileDate = GetDateTimeFromFile(f, FileType.Photo);
                    else if (fileType == FileType.Video)
                        FileDate = GetDateTimeFromFile(f, FileType.Video);
                    else
                        FileDate = GetDateTimeFromFile(f, FileType.All);

                    string foldername = FileDate.ToString("yyyy-MM-dd");
                    string filename = Fs.Name;
                    int percentage = (i + 1) * 100 / missingfiles.Count();
                    if (!FilePresent(filename, DestinationFilePath))
                    {
                        CopyPictures(f, DestinationFilePath, foldername);
                        count++;
                        BGworker_FileApp.ReportProgress(percentage, string.Format("{1}) File Name: {0}", Fs.FullName, count));
                    }
                    BGworker_FileApp.ReportProgress(percentage);
                    System.Threading.Thread.Sleep(50);
                    i++;
                }
            }
        }
        private void BGworker_FileApp_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
                Common_Progress_Bar.Value = e.ProgressPercentage;
                Percentage_lbl.Content = e.ProgressPercentage.ToString() + "%";
                if (!string.IsNullOrEmpty(e.UserState as string))
                {
                Output_Window.AppendText("\n");
                Output_Window.AppendText(e.UserState as string);
                Output_Window.ScrollToEnd();
                }
        }
        private void BGworker_FileApp_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(current_stage == stage.VERIFY)
            {
                Output_Window.AppendText("\n");
                Output_Window.AppendText("TOTAL NUMBER OF FILES NOT PRESENT: " + count);
                Output_Window.AppendText("\n");
                Output_Window.ScrollToEnd();
                verify_btn.IsEnabled = true;
                copy_btn.IsEnabled = true;
                clear_btn.IsEnabled = true;
                if (missingfiles.Count() > 0)
                {
                    string message = "Do you want to copy the Missing Files?";
                    string title = "Copy File Window";
                    MessageBoxButton buttons = MessageBoxButton.YesNo;
                    MessageBoxImage boxIcon = MessageBoxImage.Warning;
                    MessageBoxResult result = MessageBox.Show(message, title, buttons,boxIcon);
                    if (result == MessageBoxResult.Yes)
                    {
                        count = 0;
                        verify_btn.IsEnabled = false;
                        copy_btn.IsEnabled = false;
                        clear_btn.IsEnabled = false;
                        SourceFilePath = Source_txt.Text;
                        DestinationFilePath = Destination_txt.Text;
                        Output_Window.AppendText("\n");
                        Output_Window.AppendText("Starting to copy missing files :)" + "\n");
                        Output_Window.AppendText("SOURCE FOLDER PATH:\t" + Source_txt.Text + "\n");
                        Output_Window.AppendText("DESTINATION FOLDER PATH:\t" + Destination_txt.Text + "\n");
                        current_stage = stage.COPYMISSING;
                        BGworker_FileApp.RunWorkerAsync();
                    }
                    else
                    {
                        Output_Window.ScrollToEnd();
                        verify_btn.IsEnabled = true;
                        copy_btn.IsEnabled = true;
                        clear_btn.IsEnabled = true;
                    }
                }
            }
            else if(current_stage == stage.COPY || current_stage == stage.COPYMISSING)
            {
                Output_Window.AppendText("\n");
                Output_Window.AppendText("TOTAL NUMBER OF FILES COPIED: " + count);
                Output_Window.AppendText("\n");
                Output_Window.ScrollToEnd();
                verify_btn.IsEnabled = true;
                copy_btn.IsEnabled = true;
                clear_btn.IsEnabled = true;
            }
        }
        #endregion

        private void Source_open_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog Source_folder_dialog = new System.Windows.Forms.FolderBrowserDialog();
            Source_folder_dialog.Description = "Select the path of your source files";
            Source_folder_dialog.ShowNewFolderButton = false;

            MessageBoxResult result = Source_folder_dialog.ShowDialog();
            if (result.ToString() == "OK")
            {
                Source_txt.Text = Source_folder_dialog.SelectedPath;
                SourceFilePath = Source_txt.Text;
            }

        }

        private void Destination_open_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog Destination_folder_dialog = new System.Windows.Forms.FolderBrowserDialog();
            Destination_folder_dialog.Description = "Select the path of your destination files";

            MessageBoxResult result = Destination_folder_dialog.ShowDialog();
            if (result.ToString() == "OK")
            {
                Destination_txt.Text = Destination_folder_dialog.SelectedPath;
                DestinationFilePath = Destination_txt.Text;
            }

            
        }

        private void Copy_btn_Click(object sender, RoutedEventArgs e)
        {
            if(rb_photo.IsChecked == false && rb_video.IsChecked == false && rb_all.IsChecked == false)
            {
                MessageBoxImage boxIcon2 = MessageBoxImage.Warning;
                MessageBox.Show("Please chosse any one file type and retry!!!!","GG File Organizer",MessageBoxButton.OK,boxIcon2);
                return;
            }
            if(string.IsNullOrEmpty(Source_txt.Text) || string.IsNullOrEmpty(Destination_txt.Text))
            {
                MessageBoxImage boxIcon2 = MessageBoxImage.Warning;
                MessageBox.Show("Please choose the path before proceding!!!","GG File Organizer", MessageBoxButton.OK, boxIcon2);
                return;
            }

            //Alert message to the customer
            string message = string.Empty;
            if (rb_photo.IsChecked == true)
            {
                message = "You have chossen photo organizer. Do you want to proceed";
                fileType = FileType.Photo;
            }
            else if(rb_video.IsChecked == true)
            {
                message = "You have chossen video organizer. Do you want to proceed";
                fileType = FileType.Video;
            }
            else
            {
                message = "You have chossen file organizer. Do you want to proceed";
                fileType = FileType.All;
            }
            string title = "File organizer confirmation window";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxImage boxIcon = MessageBoxImage.Warning;
            MessageBoxResult result = MessageBox.Show(message, title, buttons, boxIcon);
            if (result == MessageBoxResult.Yes)
            {
                count = 0;
                verify_btn.IsEnabled = false;
                copy_btn.IsEnabled = false;
                clear_btn.IsEnabled = false;
                SourceFilePath = Source_txt.Text;
                DestinationFilePath = Destination_txt.Text;
                Output_Window.Document.Blocks.Clear();
                Output_Window.AppendText("\t\t\t\t\tWelcome to GG file organizer application");
                Output_Window.AppendText("\n");
                Output_Window.AppendText("SOURCE FOLDER PATH:\t" + Source_txt.Text + "\n");
                Output_Window.AppendText("DESTINATION FOLDER PATH:\t" + Destination_txt.Text + "\n");
                current_stage = stage.COPY;
                BGworker_FileApp.RunWorkerAsync();
            }
        }

        private void Verify_btn_Click(object sender, RoutedEventArgs e)
        {
            if (rb_photo.IsChecked == false && rb_video.IsChecked == false && rb_all.IsChecked == false)
            {
                MessageBoxImage boxIcon3 = MessageBoxImage.Warning;
                MessageBox.Show("Please chosse any one file type and retry!!!!", "GG File Organizer", MessageBoxButton.OK, boxIcon3);
                return;
            }
            if (string.IsNullOrEmpty(Source_txt.Text) || string.IsNullOrEmpty(Destination_txt.Text))
            {
                MessageBoxImage boxIcon2 = MessageBoxImage.Warning;
                MessageBox.Show("Please choose the path before proceding!!!", "GG File Organizer", MessageBoxButton.OK, boxIcon2);
                return;
            }
            //Alert message to the customer
            string message = string.Empty;
            if (rb_photo.IsChecked == true)
            {
                message = "You have chossen photo organizer. Do you want to proceed";
                fileType = FileType.Photo;
            }
            else if (rb_video.IsChecked == true)
            {
                message = "You have chossen video organizer. Do you want to proceed";
                fileType = FileType.Video;
            }
            else
            {
                message = "You have chossen file organizer. Do you want to proceed";
                fileType = FileType.All;
            }
            string title = "File organizer confirmation window";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxImage boxIcon = MessageBoxImage.Warning;
            MessageBoxResult result = MessageBox.Show(message, title, buttons, boxIcon);
            if (result == MessageBoxResult.Yes)
            {
                count = 0;
                missingfiles.Clear();
                verify_btn.IsEnabled = false;
                copy_btn.IsEnabled = false;
                clear_btn.IsEnabled = false;
                SourceFilePath = Source_txt.Text;
                DestinationFilePath = Destination_txt.Text;
                Output_Window.Document.Blocks.Clear();
                Output_Window.AppendText("\t\t\t\t\tWelcome to GG file organizer application");
                Output_Window.AppendText("\n");
                Output_Window.AppendText("SOURCE FOLDER PATH:\t" + Source_txt.Text + "\n");
                Output_Window.AppendText("DESTINATION FOLDER PATH:\t" + Destination_txt.Text + "\n");
                Output_Window.ScrollToEnd();
                current_stage = stage.VERIFY;
                BGworker_FileApp.RunWorkerAsync();
            }
        }

        private void Clear_btn_Click(object sender, RoutedEventArgs e)
        {
            SourceFilePath = string.Empty;
            Source_txt.Text = string.Empty;
            DestinationFilePath = string.Empty;
            Destination_txt.Text = string.Empty;
            rb_photo.IsChecked = false;
            rb_video.IsChecked = false;
            rb_all.IsChecked = false;
            Common_Progress_Bar.Value = 0;
            Percentage_lbl.Content = "0%";
            count = 0;
            missingfiles.Clear();
            Output_Window.Document.Blocks.Clear();
            fileType = FileType.All;
            Output_Window.AppendText("\t\t\t\t\tWelcome to GG file organizer application\n");
        }

        private void Console_Save_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog Save_File_dialog = new System.Windows.Forms.SaveFileDialog();
            Save_File_dialog.Title = "Save the Console log to File";
            Save_File_dialog.Filter = "Log(*.log) | *.log";
            MessageBoxResult result = Save_File_dialog.ShowDialog();
            if (result.ToString() == "OK")
            {
                string path = Save_File_dialog.FileName;
                string text = new TextRange(Output_Window.Document.ContentStart, Output_Window.Document.ContentEnd).Text;
                SaveData(text, path);
            }
        }

        private void File_propertie_Menu_Click(object sender, RoutedEventArgs e)
        {
            File_Properties fp = new File_Properties();
            fp.ShowDialog();
        }

        private void Exit_Menu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
