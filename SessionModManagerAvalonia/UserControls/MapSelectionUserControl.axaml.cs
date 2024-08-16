using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using Avalonia.Input;
using MsBox.Avalonia;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using SessionMapSwitcherCore.Utils;
using SessionModManagerAvalonia.Windows;
using Avalonia.Controls.ApplicationLifetimes;
using SessionModManagerAvalonia.Classes;

namespace SessionModManagerAvalonia;

public partial class MapSelectionUserControl : UserControl
{
    public readonly MapSelectionViewModel ViewModel;

    public MapSelectionUserControl()
    {
        InitializeComponent();

        InitPathIfExists();

        ViewModel = new MapSelectionViewModel();
        ViewModel.ReloadAvailableMapsInBackground();

        this.DataContext = ViewModel;
    }

    private async void BtnBrowseSessionPath_Click(object sender, RoutedEventArgs e)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Session Folder",
            AllowMultiple = false,
        });

        if (folders.Any())
        {
            await SetAndValidateSessionPath(folders.FirstOrDefault().TryGetLocalPath());
        }
    }

    private async void BtnStartGame_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SessionPath.IsSessionRunning())
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Session is already running! Click 'Yes' if you want to restart the game.", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Info);
            var result = await box.ShowAsync();

            if (result == MsBox.Avalonia.Enums.ButtonResult.No)
            {
                return;
            }
            else
            {
                // kill the process
                Process[] procs = System.Diagnostics.Process.GetProcessesByName("SessionGame-Win64-Shipping");
                if (procs.Length > 0)
                {
                    procs[0].Kill();
                }
            }
        }

        await LoadMapInBackgroundAndContinueWith((antecedent) =>
        {
            ViewModel.InputControlsEnabled = true;

            ViewModel.StartGameAndLoadSecondMap();
        });
    }

    private async Task LoadMap()
    {
        try
        {
            // double check the controls are disabled and should not load (e.g. when double clicking map in list)
            if (ViewModel.InputControlsEnabled == false)
            {
                return;
            }

            await LoadMapInBackgroundAndContinueWith((antecedent) =>
            {
                if (antecedent.IsFaulted)
                {
                    MessageService.Instance.ShowMessage($"failed to load map: {antecedent.Exception.InnerException?.Message}");
                }

                ViewModel.InputControlsEnabled = true;
            });
        }
        catch (AggregateException ae)
        {
            MessageService.Instance.ShowMessage($"failed to load: {ae.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            MessageService.Instance.ShowMessage($"failed to load: {ex.Message}");
        }
    }

    private async Task LoadMapInBackgroundAndContinueWith(Action<Task> continuationTask)
    {
        if (SessionPath.IsSessionPathValid() == false)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error!", "You have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowAsync();
            return;
        }

        if (UeModUnlocker.IsGamePatched() == false)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Session has not been patched yet. Click 'Patch With Illusory Mod Unlocker' to patch the game.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            await box.ShowAsync();
            return;
        }

        if (lstMaps.SelectedItem == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Select a map to load first!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }


        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;

        if (selectedItem.IsValid == false)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error!", "This map is missing the required Game Mode Override 'PBP_InGameSessionGameMode'.\n\nAdd a Game Mode to your map in UE4: '/Content/Data/PBP_InGameSessionGameMode.uasset'.\nThen reload the list of available maps.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            await box.ShowAsync();
            return;
        }

        MessageService.Instance.ShowMessage($"Loading {selectedItem.MapName} ...");
        ViewModel.InputControlsEnabled = false;

        Task t = Task.Run(() => ViewModel.LoadSelectedMap(selectedItem));

        await t.ContinueWith(continuationTask);
    }

    private async void BtnReloadMaps_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ReloadAvailableMapsInBackground();
    }

    private async void TxtSessionPath_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            await SetAndValidateSessionPath(ViewModel.SessionPathTextInput); // the viewmodel is 2-way binded so the new path value is already set when enter is pressed so we pass the same value to store in app setttings and validate it
            MessageService.Instance.ShowMessage("Session Path updated!");
        }
    }

    private async Task SetAndValidateSessionPath(string path)
    {
        ViewModel.SetSessionPath(path); // this will save it to app settings
        ViewModel.SetCurrentlyLoadedMap();

        if (SessionPath.IsSessionPathValid())
        {
            ViewModel.ReloadAvailableMapsInBackground();
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Warning!", "You may have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            await box.ShowAsync();
        }
    }

    private void MenuOpenSessionFolder_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenFolderToSession();
    }

    private void MenuOpenMapsFolder_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenFolderToSessionContent();
    }

    private void MenuOpenSelectedMapFolder_Click(object sender, RoutedEventArgs e)
    {
        if (lstMaps.SelectedItem == null)
        {
            MessageService.Instance.ShowMessage("Cannot open folder: No map selected.");
            return;
        }

        MapListItem selectedMap = lstMaps.SelectedItem as MapListItem;
        ViewModel.OpenFolderToSelectedMap(selectedMap);
    }

    private async void ChkShowInvalidMaps_Click(object sender, RoutedEventArgs e)
    {
        //ViewModel.FilteredAvailableMaps.Refresh();
    }

    private void MenuOpenReadme_Click(object sender, RoutedEventArgs e)
    {
        OpenReadMeInBrowser();
    }

    private static void OpenReadMeInBrowser()
    {
        ProcessStartInfo info = new ProcessStartInfo()
        {
            FileName = "https://github.com/rodriada000/SessionMapSwitcher/blob/master/README.md",
            UseShellExecute = true,
        };

        Process.Start(info);
    }

    private void BtnImportMap_Click(object sender, RoutedEventArgs e)
    {
        OpenComputerImportWindow();
    }

    internal void OpenComputerImportWindow()
    {
        if (SessionPath.IsSessionPathValid() == false)
        {
            MessageService.Instance.ShowMessage("Cannot import: You must set your path to Session before importing maps.");
            return;
        }

        MapImportViewModel importViewModel = new MapImportViewModel();

        ComputerImportWindow importWindow = new ComputerImportWindow(importViewModel);

        importWindow.Closed += (s, e) =>
        {
            ViewModel.ReloadAvailableMapsInBackground(); // reload list of available maps as it may have changed
        };

        importWindow.Show();
    }

    private async void MenuReimporSelectedMap_Click(object sender, RoutedEventArgs e)
    {
        if (lstMaps.SelectedItem == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Select a map to load first!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }

        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;

        ViewModel.ReimportMapFiles(selectedItem);
    }

    private void ContextMenu_ContextMenuOpening(object sender, EventArgs e)
    {
        // disable certain menu items if no map selected
        bool isMapSelected = (lstMaps.SelectedItem != null);
        bool isSessionPathValid = SessionPath.IsSessionPathValid();

        menuReimporSelectedMap.IsEnabled = isMapSelected;
        menuOpenSelectedMapFolder.IsEnabled = isMapSelected;
        menuRenameSelectedMap.IsEnabled = isMapSelected;
        menuHideSelectedMap.IsEnabled = isMapSelected;

        menuOpenSessionFolder.IsEnabled = isSessionPathValid;
        menuOpenMapsFolder.IsEnabled = isSessionPathValid;

        if (isMapSelected)
        {
            MapListItem selected = (lstMaps.SelectedItem as MapListItem);
            bool hasImportLocation = MetaDataManager.IsImportLocationStored(selected);
            menuReimporSelectedMap.IsEnabled = hasImportLocation;
            menuHideSelectedMap.Header = selected.IsHiddenByUser ? "Show Selected Map" : "Hide Selected Map";


            if (ViewModel.SecondMapToLoad == null || ViewModel.SecondMapToLoad?.FullPath != selected.FullPath)
            {
                menuSecondMapToLoad.Header = "Set As Second Map To Load (When Leaving Apartment)";
            }
            else
            {
                menuSecondMapToLoad.Header = "Clear As Second Map To Load";
            }


            bool canBeDeleted = MetaDataManager.HasPathToMapFilesStored(selected);
            menuDeleteSelectedMap.IsEnabled = canBeDeleted;
        }
    }


    /// <summary>
    /// Opens the window to rename a map. 
    /// </summary>
    private async void MenuRenameSelectedMap_Click(object sender, RoutedEventArgs e)
    {
        if (lstMaps.SelectedItem == null)
        {

            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Select a map to rename first!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }


        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;
        OpenRenameMapWindow(selectedItem);
    }

    private async void MenuHideSelectedMap_Click(object sender, RoutedEventArgs e)
    {
        if (lstMaps.SelectedItem == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Select a map to hide/show first!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }

        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;
        ViewModel.ToggleVisiblityOfMap(selectedItem);
    }

    //private void TxtSessionPath_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
    //{
    //    //e.Handled = true;

    //    //if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, false) == true)
    //    //{
    //    //    e.Effects = System.Windows.DragDropEffects.All;
    //    //}
    //}

    //private void TxtSessionPath_PreviewDrop(object sender, System.Windows.DragEventArgs e)
    //{
    //    //string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
    //    //if (files != null && files.Length != 0)
    //    //{
    //    //    ViewModel.SessionPathTextInput = files[0];
    //    //}
    //}

    /// <summary>
    /// Opens a window to enter a new name for a map.
    /// Writes to meta data file if user clicks 'Rename' in window.
    /// </summary>
    /// <param name="selectedMap"></param>
    internal async Task OpenRenameMapWindow(MapListItem selectedMap)
    {
        RenameMapViewModel viewModel = new RenameMapViewModel(selectedMap);
        RenameMapWindow window = new RenameMapWindow(viewModel)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        bool result = await window.ShowDialog<bool>((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);

        if (result)
        {
            bool didWrite = MetaDataManager.WriteCustomMapPropertiesToFile(selectedMap);

            if (didWrite == false)
            {
                MessageService.Instance.ShowMessage("Failed to update .meta file with new custom name. Your custom name may have not been saved and will be lost when the app restarts.");
                return;
            }

            ViewModel.RefreshFilteredMaps();
            MessageService.Instance.ShowMessage($"{selectedMap.MapName} renamed to {selectedMap.CustomName}!");
        }
    }


    private async void menuDeleteSelectedMap_Click(object sender, RoutedEventArgs e)
    {
        if (lstMaps.SelectedItem == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Select a map to delete first!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }

        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;

        var warnBox = MessageBoxManager.GetMessageBoxStandard("Warning!", $"Are you sure you want to delete {selectedItem.DisplayName}?", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning);
        var result = await warnBox.ShowAsync();

        if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
        {
            ViewModel.DeleteSelectedMap(selectedItem);
        }
    }

    private async void menuSecondMapToLoad_Click(object sender, RoutedEventArgs e)
    {
        if (lstMaps.SelectedItem == null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Select a map first!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await box.ShowAsync();
            return;
        }


        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;
        ViewModel.SetOrClearSecondMapToLoad(selectedItem);
    }

    private void menuOpenSaveFolder_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenFolderToSaveFiles();
    }

    private void lstMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;
        ViewModel.GetSelectedPreviewImageAsync(selectedItem);
    }

    private async void btnLoadMap_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await LoadMap();
    }

    private async void MapListItem_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        await LoadMap();
    }

    private void InitPathIfExists()
    {
        string sessionPath = AppSettingsUtil.GetAppSetting(SettingKey.PathToSession);

        if (OperatingSystem.IsWindows() && string.IsNullOrEmpty(sessionPath))
        {
            sessionPath = RegistryHelper.GetPathFromRegistry();
        }

        SessionPath.ToSession = sessionPath;

        if (!string.IsNullOrWhiteSpace(sessionPath) && SessionPath.IsSessionPathValid() && string.IsNullOrWhiteSpace(AppSettingsUtil.GetAppSetting(SettingKey.LaunchViaSteam)))
        {
            // set the steam launch setting based on path
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.LaunchViaSteam, sessionPath.Contains("steamapps").ToString());
        }

#if DEBUG
        NLog.LogManager.ThrowExceptions = true;
#endif


    }

    private void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;
        menuHideSelectedMap.Header = selectedItem?.IsHiddenByUser == true ? "Show Selected Map" : "Hide Selected Map";
    }
}