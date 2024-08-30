using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using NLog;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SessionModManagerAvalonia;

public partial class TextureReplacerUserControl : UserControl
{
    public TextureReplacerViewModel ViewModel { get; set; }

    public TextureReplacerUserControl()
    {
        InitializeComponent();

        ViewModel = new TextureReplacerViewModel();
        ViewModel.LoadInstalledTextures();

        this.DataContext = ViewModel;

        AddHandler(DragDrop.DropEvent, TextBox_PreviewDrop);
    }

    private void BtnReplace_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsReplaceButtonEnabled = false;

        Task.Factory.StartNew(() =>
        {
            ViewModel.ImportTextureMod();
            ViewModel.LoadInstalledTextures();
        }).ContinueWith(result =>
        {
            ViewModel.IsReplaceButtonEnabled = true;
        });
    }

    private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        await BrowseForFiles();
    }

    internal async Task BrowseForFiles()
    {
        var topLevel = TopLevel.GetTopLevel(this);

        IReadOnlyList<FilePickerFileType> filters = new List<FilePickerFileType>() { new FilePickerFileType("*.uasset *.zip *.rar file") { Patterns = new List<string>() { "*.uasset", "*.zip", "*.rar" } } };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select .uasset Texture File, .zip, or .rar File Containing Texture Files",
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        if (files.Any())
        {
            ViewModel.PathToFile = files.FirstOrDefault().TryGetLocalPath();
        }
    }

    private void btnRemove_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTexture == null)
        {
            MessageService.Instance.ShowMessage("Select a mod to remove first!");
            return;
        }

        ViewModel.DeleteSelectedMod();
    }

    /// <summary>
    /// logic when dropping file into textbox to extract path
    /// </summary>
    private void TextBox_PreviewDrop(object sender, DragEventArgs e)
    {
        var files = (IEnumerable<IStorageItem>)e.Data.Get(DataFormats.Files);
        if (files != null && files.Count() != 0)
        {
            ViewModel.PathToFile = files.FirstOrDefault().TryGetLocalPath();
        }
    }

    private void MenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel.SelectedTexture == null)
        {
            return;
        }

        try
        {
            if (!Directory.Exists(ViewModel.SelectedTexture.MetaData.FolderInstallPath))
            {
                MessageService.Instance.ShowMessage($"Cannot open folder because it is missing. You may need to redownload the mod.");
                return;
            }

            Process.Start(new ProcessStartInfo(ViewModel.SelectedTexture.MetaData.FolderInstallPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageService.Instance.ShowMessage($"Cannot open folder: {ex.Message}");
        }
    }

    private void MenuItemInstalledFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(SessionPath.PathToInstalledModsFolder))
            {
                MessageService.Instance.ShowMessage($"Cannot open folder because it is missing. there may be no mods installed.");
                return;
            }

            Process.Start(new ProcessStartInfo(SessionPath.PathToInstalledModsFolder) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageService.Instance.ShowMessage($"Cannot open folder: {ex.Message}");
        }
    }


    
}