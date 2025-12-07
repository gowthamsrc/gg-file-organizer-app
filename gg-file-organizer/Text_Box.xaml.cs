using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls.Primitives;

namespace gg_file_organizer.Controls
{
    /// <summary>
    /// Interaction logic for Text_Box.xaml
    /// </summary>
    public partial class Text_Box : System.Windows.Controls.TextBox
    {
        private Popup? Popup => this.Template.FindName("PART_Popup", this) as Popup;
        private System.Windows.Controls.ListBox? ItemList => this.Template.FindName("PART_ItemList", this) as System.Windows.Controls.ListBox;
        private Grid? Root => this.Template.FindName("root", this) as Grid;
        //12-25-08 : Add Ghost image when picking from ItemList
        //TextBlock TempVisual { get { return this.Template.FindName("PART_TempVisual", this) as TextBlock; } }
        private ScrollViewer? Host => this.Template.FindName("PART_ContentHost", this) as ScrollViewer;
        private UIElement? TextBoxView
        {
            get
            {
                if (Host == null) return null;
                foreach (object o in LogicalTreeHelper.GetChildren(Host))
                    return o as UIElement;
                return null;
            }
        }

        private bool _loaded = false;
        private string? _lastPath;
        
        public Text_Box()
        {
            InitializeComponent();
        }
        
        private bool _prevState = false;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _loaded = true;
            this.KeyDown += new System.Windows.Input.KeyEventHandler(AutoCompleteTextBox_KeyDown);
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(AutoCompleteTextBox_PreviewKeyDown);
            
            if (ItemList != null)
            {
                ItemList.PreviewMouseDown += new MouseButtonEventHandler(ItemList_PreviewMouseDown);
                ItemList.KeyDown += new System.Windows.Input.KeyEventHandler(ItemList_KeyDown);
            }
            
            //TempVisual.MouseDown += new MouseButtonEventHandler(TempVisual_MouseDown);
            //09-04-09 Based on SilverLaw's approach 
            if (Popup != null)
            {
                Popup.CustomPopupPlacementCallback += new CustomPopupPlacementCallback(Repositioning);
            }

            Window? parentWindow = GetParentWindow();
            if (parentWindow != null && Popup != null)
            {
                var popup = Popup;
                parentWindow.Deactivated += delegate { _prevState = popup.IsOpen; popup.IsOpen = false; };
                parentWindow.Activated += delegate { popup.IsOpen = _prevState; };
            }
        }

        private Window? GetParentWindow()
        {
            DependencyObject? d = this;
            while (d != null && d is not Window)
                d = LogicalTreeHelper.GetParent(d);
            return d as Window;
        }

        //09-04-09 Based on SilverLaw's approach 
        private CustomPopupPlacement[] Repositioning(System.Windows.Size popupSize, System.Windows.Size targetSize, System.Windows.Point offset)
        {
            var rootHeight = Root?.ActualHeight ?? 0;
            return new CustomPopupPlacement[] {
                new CustomPopupPlacement(new System.Windows.Point((0.01 - offset.X), (rootHeight - offset.Y)), PopupPrimaryAxis.None) };
        }

        private void TempVisual_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string text = Text;
            if (ItemList != null)
                ItemList.SelectedIndex = -1;
            Text = text;
            if (Popup != null)
                Popup.IsOpen = false;
        }

        private void AutoCompleteTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //12-25-08 - added PageDown Support
            if (ItemList != null && ItemList.Items.Count > 0 && e.OriginalSource is not ListBoxItem)
            {
                switch (e.Key)
                {
                    case Key.Up:
                    case Key.Down:
                    case Key.Prior:
                    case Key.Next:
                        ItemList.Focus();
                        ItemList.SelectedIndex = 0;
                        if (ItemList.ItemContainerGenerator.ContainerFromIndex(ItemList.SelectedIndex) is ListBoxItem lbi)
                        {
                            lbi.Focus();
                        }
                        e.Handled = true;
                        break;
                }
            }
        }

        private void ItemList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.OriginalSource is ListBoxItem tb)
            {
                e.Handled = true;
                switch (e.Key)
                {
                    case Key.Enter:
                        Text = tb.Content as string ?? string.Empty;
                        UpdateSource();
                        break;
                    //12-25-08 - added "\" support when picking in list view
                    case Key.Oem5:
                        Text = (tb.Content as string ?? string.Empty) + "\\";
                        break;
                    //12-25-08 - roll back if escape is pressed
                    case Key.Escape:
                        Text = (_lastPath?.TrimEnd('\\') ?? string.Empty) + "\\";
                        break;
                    default:
                        e.Handled = false;
                        break;
                }
                //12-25-08 - Force focus back the control after selected.
                if (e.Handled)
                {
                    Keyboard.Focus(this);
                    if (Popup != null)
                        Popup.IsOpen = false;
                    this.Select(Text.Length, 0); //Select last char
                }
            }
        }

        private void AutoCompleteTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Popup != null)
                    Popup.IsOpen = false;
                UpdateSource();
            }
        }

        private void UpdateSource()
        {
            this.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        }

        private void ItemList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.OriginalSource is TextBlock tb)
                {
                    Text = tb.Text;
                    UpdateSource();
                    if (Popup != null)
                        Popup.IsOpen = false;
                    e.Handled = true;
                }
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (_loaded && ItemList != null && Popup != null)
            {
                try
                {
                    //if (lastPath != Path.GetDirectoryName(this.Text))
                    //if (textBox.Text.EndsWith("\\"))                        
                    {
                        _lastPath = Path.GetDirectoryName(this.Text);
                        string[] paths = Lookup(this.Text);

                        ItemList.Items.Clear();
                        foreach (string path in paths)
                            if (!string.Equals(path, this.Text, StringComparison.CurrentCultureIgnoreCase))
                                ItemList.Items.Add(path);
                    }

                    Popup.IsOpen = ItemList.Items.Count > 0;

                    //ItemList.Items.Filter = p =>
                    //{
                    //    string path = p as string;
                    //    return path.StartsWith(this.Text, StringComparison.CurrentCultureIgnoreCase) &&
                    //        !(String.Equals(path, this.Text, StringComparison.CurrentCultureIgnoreCase));
                    //};
                }
                catch
                {
                    // Silently handle path lookup errors
                }
            }
        }

        private string[] Lookup(string path)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directoryName) && Directory.Exists(directoryName))
                {
                    DirectoryInfo lookupFolder = new DirectoryInfo(directoryName);
                    DirectoryInfo[] allItems = lookupFolder.GetDirectories();
                    return (from di in allItems 
                            where di.FullName.StartsWith(path, StringComparison.CurrentCultureIgnoreCase) 
                            select di.FullName).ToArray();
                }
            }
            catch (UnauthorizedAccessException)
            {
                // User doesn't have access to this directory
            }
            catch (IOException)
            {
                // Directory may have been removed
            }
            return Array.Empty<string>();
        }
    }
}
