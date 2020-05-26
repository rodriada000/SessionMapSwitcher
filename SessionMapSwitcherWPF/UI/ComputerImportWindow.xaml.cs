using SessionModManagerCore.ViewModels;
using System.Windows;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for ComputerImportWindow.xaml
    /// </summary>
    public partial class ComputerImportWindow : Window
    {
        private ComputerImportViewModel ViewModel { get; set; }

        public ComputerImportWindow()
        {
            InitializeComponent();
        }

        public ComputerImportWindow(ComputerImportViewModel importViewModel)
        {
            InitializeComponent();

            this.ViewModel = importViewModel;
            this.DataContext = this.ViewModel;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFolderOrFile();
        }

        private void BtnImportMap_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.BeginImportMapAsync();
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void TextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length != 0)
            {
                ViewModel.PathInput = files[0];
            }
        }

        internal void BrowseForFolderOrFile()
        {
            if (ViewModel.IsZipFileImport)
            {
                using (System.Windows.Forms.OpenFileDialog fileBrowserDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    fileBrowserDialog.Filter = "Zip/Rar files (*.zip *.rar)|*.zip;*.rar|All files (*.*)|*.*";
                    fileBrowserDialog.Title = "Select .zip or .rar File Containing Session Map";
                    System.Windows.Forms.DialogResult result = fileBrowserDialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        ViewModel.PathInput = fileBrowserDialog.FileName;
                    }
                }
            }
            else
            {
                using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderBrowserDialog.ShowNewFolderButton = false;
                    folderBrowserDialog.Description = "Select Folder Containing Session Map Files";
                    System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        ViewModel.PathInput = folderBrowserDialog.SelectedPath;
                    }
                }
            }
        }
    }
}
