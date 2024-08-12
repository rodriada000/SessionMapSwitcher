using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.ViewModels;
using System.ComponentModel;

namespace SessionModManagerAvalonia;

public partial class AssetStoreUserControl : UserControl
{
    public AssetStoreViewModel ViewModel { get; set; }

    public AssetStoreUserControl()
    {
        InitializeComponent();

        ViewModel = new AssetStoreViewModel();
        this.DataContext = ViewModel;
    }

    private async void btnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedAsset == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Select an asset to install first.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            var result = await box.ShowAsync();
            return;
        }

        if (ViewModel.SelectedAsset.IsOutOfDate)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Warning - Possible Old Mod Detected", "This mod was last updated before the Session 0.0.0.5 game update. Installing this mod may crash your game with the following error:\n\n\"Corrupt data found, please verify your installation.\"\n\nUninstall all old mods to fix the above error.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            var result = await box.ShowAsync();
        }

        ViewModel.DownloadSelectedAssetAsync();
    }

    private void btnRemove_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RemoveSelectedAsset();
    }

    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.CheckForCatalogUpdatesAsync();
    }

    private void menuItemCancelDownload_Click(object sender, RoutedEventArgs e)
    {
        DownloadItemViewModel downloadItem = lstDownloads.SelectedItem as DownloadItemViewModel;

        if (downloadItem == null)
        {
            return;
        }

        downloadItem.IsCanceled = true;
        ViewModel.CancelDownload(downloadItem);
    }

    private void btnManageCat_Click(object sender, RoutedEventArgs e)
    {
        ManageCatalogWindow catalogWindow = new ManageCatalogWindow();
        catalogWindow.Show();
        ViewModel.CheckForCatalogUpdatesAsync();
    }

    private void menuItemBrowserDownload_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.LaunchDownloadInBrowser();
    }

    private async void menuItemFetchImages_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.DownloadAllPreviewImagesAsync();
    }

    private void menuItemCancelAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelAllDownloads();
    }
}