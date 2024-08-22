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

        AddHandler(DragDrop.DropEvent, TextBox_PreviewDrop);
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

    private void TextBox_PreviewDrop(object sender, DragEventArgs e)
    {
        var files = (IEnumerable<IStorageItem>)e.Data.Get(DataFormats.Files);
        if (files != null && files.Count() != 0)
        {
            ViewModel.PathInput = files.FirstOrDefault().TryGetLocalPath();
        }
    }

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
        }
    }
}