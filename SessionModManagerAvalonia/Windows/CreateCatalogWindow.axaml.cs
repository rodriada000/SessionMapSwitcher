using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using NLog;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SessionModManagerAvalonia;

public partial class CreateCatalogWindow : Window
{
    CreateCatalogViewModel ViewModel { get; set; }

    public CreateCatalogWindow()
    {
        InitializeComponent();

        ViewModel = new CreateCatalogViewModel();
        this.DataContext = ViewModel;

        ViewModel.UpdatedAssetInvalid += ViewModel_UpdatedAssetInvalid;
    }

    private void ViewModel_UpdatedAssetInvalid(string validationMessage)
    {
        Task.Factory.StartNew(async () =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Failed to Update", $"The following errors were found:\n\n{validationMessage}", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            var result = await box.ShowAsync();
        });
        ViewModel.SelectedAsset = ViewModel.SelectedAsset;
    }

    private async void btnImport_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);

        IReadOnlyList<FilePickerFileType> filters = new List<FilePickerFileType>() { new FilePickerFileType("Catalog Json (*.json)") { Patterns = new List<string>() { "*.json" } } };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Asset Catalog Json File",
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        if (files.Any())
        {
            BoolWithMessage didImport = ViewModel.ImportCatalog(files.First().TryGetLocalPath());

            if (!didImport.Result)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Failed to Import", didImport.Message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                var result = await box.ShowAsync();
            }
        }
    }

    private async void btnExport_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);

        IReadOnlyList<FilePickerFileType> filters = new List<FilePickerFileType>() { new FilePickerFileType("Catalog Json (*.json)") { Patterns = new List<string>() { "*.json" } } };
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save Asset Catalog Json File"
        });

        if (!string.IsNullOrWhiteSpace(file?.TryGetLocalPath()))
        {
            BoolWithMessage didSave = ViewModel.ExportCatalog(file?.TryGetLocalPath());

            if (!didSave.Result)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Failed to Export", didSave.Message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                var result = await box.ShowAsync();
            }
        }
    }

    private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteAsset(ViewModel.SelectedAsset);
    }

    private async void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        BoolWithMessage didAdd = ViewModel.AddAsset();

        if (didAdd.Result)
        {
            //lstAssets.ScrollIntoView(ViewModel.SelectedAsset);
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Failed to Add", didAdd.Message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            var result = await box.ShowAsync();
        }
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        ViewModel.UpdatedAssetInvalid -= ViewModel_UpdatedAssetInvalid;
    }
}