using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using SessionModManagerCore.ViewModels;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SessionModManagerAvalonia.Windows;

public partial class ComputerImportWindow : Window
{
    private MapImportViewModel ViewModel { get; set; }

    public ComputerImportWindow()
    {
        InitializeComponent();
    }

    public ComputerImportWindow(MapImportViewModel importViewModel)
    {
        InitializeComponent();

        ViewModel = importViewModel;
        DataContext = ViewModel;
    }

    private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        await BrowseForFolderOrFile();
    }

    private void BtnImportMap_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.BeginImportMapAsync();
    }

    //private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    //{
    //    e.Handled = true;

    //    if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
    //    {
    //        e.Effects = DragDropEffects.All;
    //    }
    //}

    //private void TextBox_PreviewDrop(object sender, DragEventArgs e)
    //{
    //    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
    //    if (files != null && files.Length != 0)
    //    {
    //        ViewModel.PathInput = files[0];
    //    }
    //}

    internal async Task BrowseForFolderOrFile()
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = GetTopLevel(this);

        if (ViewModel.IsZipFileImport)
        {
            IReadOnlyList<FilePickerFileType> filters = new List<FilePickerFileType>() { new FilePickerFileType("Zip/Rar files (*.zip *.rar)") { Patterns = new List<string>() { "*.zip", "*.rar" } } };
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select .zip or .rar File Containing Session Map",
                AllowMultiple = false,
                FileTypeFilter = filters
            });

            if (files.Any())
            {
                ViewModel.PathInput = files.FirstOrDefault().TryGetLocalPath();
            }
            //using (System.Windows.Forms.OpenFileDialog fileBrowserDialog = new System.Windows.Forms.OpenFileDialog())
            //{
            //    fileBrowserDialog.Filter = "Zip/Rar files (*.zip *.rar)|*.zip;*.rar|All files (*.*)|*.*";
            //    fileBrowserDialog.Title = "Select .zip or .rar File Containing Session Map";
            //    System.Windows.Forms.DialogResult result = fileBrowserDialog.ShowDialog();
            //    if (result == System.Windows.Forms.DialogResult.OK)
            //    {
            //        ViewModel.PathInput = fileBrowserDialog.FileName;
            //    }
            //}
        }
        else
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder Containing Session Map Files",
                AllowMultiple = false,
            });

            if (folders.Any())
            {
                ViewModel.PathInput = folders.FirstOrDefault().TryGetLocalPath();
            }

            //using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            //{
            //    folderBrowserDialog.ShowNewFolderButton = false;
            //    folderBrowserDialog.Description = "Select Folder Containing Session Map Files";
            //    System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
            //    if (result == System.Windows.Forms.DialogResult.OK)
            //    {
            //        ViewModel.PathInput = folderBrowserDialog.SelectedPath;
            //    }
            //}
        }
    }
}