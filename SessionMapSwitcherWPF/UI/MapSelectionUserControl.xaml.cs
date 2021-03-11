using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcher.Classes.Events;
using SessionMapSwitcher.UI;
using System.Windows.Threading;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherWPF.Classes;
using System.Windows.Controls;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using SessionModManagerWPF.UI;
using SessionMapSwitcher;

namespace SessionModManagerWPF.UI
{
    /// <summary>
    /// Interaction logic for MapSelectionUserControl.xaml
    /// </summary>
    public partial class MapSelectionUserControl : UserControl
    {
        public readonly MapSelectionViewModel ViewModel;

        public MapSelectionUserControl()
        {
            InitializeComponent();

            ViewModel = new MapSelectionViewModel();
            ViewModel.ReloadAvailableMapsInBackground();

            this.DataContext = ViewModel;
        }

        private void BtnBrowseSessionPath_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    SetAndValidateSessionPath(folderBrowserDialog.SelectedPath);
                }
            }
        }

        private void BtnStartGame_Click(object sender, RoutedEventArgs e)
        {
            if (SessionPath.IsSessionRunning())
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Session is already running! Click 'Yes' if you want to restart the game.", "Notice!", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No);

                if (result == MessageBoxResult.No)
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

            LoadMapInBackgroundAndContinueWith((antecedent) =>
            {
                ViewModel.InputControlsEnabled = true;

                ViewModel.StartGameAndLoadSecondMap();
            });
        }

        private void BtnLoadMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // double check the controls are disabled and should not load (e.g. when double clicking map in list)
                if (ViewModel.InputControlsEnabled == false)
                {
                    return;
                }

                LoadMapInBackgroundAndContinueWith((antecedent) =>
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

        private void LoadMapInBackgroundAndContinueWith(Action<Task> continuationTask)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                System.Windows.MessageBox.Show("You have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (UeModUnlocker.IsGamePatched() == false)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Session has not been patched yet. Click 'Patch With Illusory Mod Unlocker' to patch the game.", "Notice!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map to load first!",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                return;
            }


            MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;

            if (selectedItem.IsValid == false)
            {
                System.Windows.MessageBox.Show("This map is missing the required Game Mode Override 'PBP_InGameSessionGameMode'.\n\nAdd a Game Mode to your map in UE4: '/Content/Data/PBP_InGameSessionGameMode.uasset'.\nThen reload the list of available maps.",
                                                "Error!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Error);
                return;
            }

            MessageService.Instance.ShowMessage($"Loading {selectedItem.MapName} ...");
            ViewModel.InputControlsEnabled = false;

            Task t = Task.Run(() => ViewModel.LoadSelectedMap(selectedItem));

            t.ContinueWith(continuationTask);
        }

        private void BtnReloadMaps_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ReloadAvailableMapsInBackground();
        }

        /// <summary>
        /// Load map when an available map is double clicked
        /// </summary>
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnLoadMap_Click(sender, e);
        }

        private void TxtSessionPath_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SetAndValidateSessionPath(ViewModel.SessionPathTextInput); // the viewmodel is 2-way binded so the new path value is already set when enter is pressed so we pass the same value to store in app setttings and validate it
                MessageService.Instance.ShowMessage("Session Path updated!");
            }
        }

        private void SetAndValidateSessionPath(string path)
        {
            ViewModel.SetSessionPath(path); // this will save it to app settings
            ViewModel.SetCurrentlyLoadedMap();

            if (SessionPath.IsSessionPathValid())
            {
                ViewModel.ReloadAvailableMapsInBackground();
            }
            else
            {
                System.Windows.MessageBox.Show("You may have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void ChkShowInvalidMaps_Click(object sender, RoutedEventArgs e)
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
                FileName = "https://github.com/rodriada000/SessionMapSwitcher/blob/master/README.md"
            };

            Process.Start(info);
        }

        private void BtnImportMap_Click(object sender, RoutedEventArgs e)
        {
            if (UeModUnlocker.IsGamePatched() == false)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Session has not been patched yet. Click 'Patch With Illusory Mod Unlocker' to patch the game.", "Notice!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            importContextMenu.IsOpen = true;
        }

        private void MenuComputerImport_Click(object sender, RoutedEventArgs e)
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

            ComputerImportViewModel importViewModel = new ComputerImportViewModel();

            ComputerImportWindow importWindow = new ComputerImportWindow(importViewModel);
            importWindow.ShowDialog();

            ViewModel.ReloadAvailableMapsInBackground(); // reload list of available maps as it may have changed
        }

        private void MenuReimporSelectedMap_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map to load first!",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                return;
            }


            MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;

            ViewModel.ReimportMapFiles(selectedItem);
        }

        private void ContextMenu_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
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
                menuReimporSelectedMap.ToolTip = hasImportLocation ? null : "You can only re-import if you imported the map from 'Import Map > From Computer ...' and imported a folder.\n(does not work with .zip files)";
                menuHideSelectedMap.Header = selected.IsHiddenByUser ? "Show Selected Map" : "Hide Selected Map";


                if (ViewModel.SecondMapToLoad == null || ViewModel.SecondMapToLoad?.FullPath != selected.FullPath)
                {
                    menuSecondMapToLoad.Header = "Set As Second Map To Load (When Leaving Apartment)";
                    menuSecondMapToLoad.ToolTip = "Set the map to be loaded after you leave the apartment (before starting the game)";
                }
                else
                {
                    menuSecondMapToLoad.ToolTip = "This will clear the selected map to not load after you leave the apartment";
                    menuSecondMapToLoad.Header = "Clear As Second Map To Load";
                }


                bool canBeDeleted = MetaDataManager.HasPathToMapFilesStored(selected);
                menuDeleteSelectedMap.IsEnabled = canBeDeleted;
            }
        }


        /// <summary>
        /// Opens the window to rename a map. 
        /// </summary>
        private void MenuRenameSelectedMap_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map to rename first!",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                return;
            }


            MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;
            OpenRenameMapWindow(selectedItem);
        }

        private void MenuHideSelectedMap_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map to hide/show first!",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                return;
            }

            MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;
            ViewModel.ToggleVisiblityOfMap(selectedItem);
        }

        private void TxtSessionPath_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;

            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, false) == true)
            {
                e.Effects = System.Windows.DragDropEffects.All;
            }
        }

        private void TxtSessionPath_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files != null && files.Length != 0)
            {
                ViewModel.SessionPathTextInput = files[0];
            }
        }

        /// <summary>
        /// Opens a window to enter a new name for a map.
        /// Writes to meta data file if user clicks 'Rename' in window.
        /// </summary>
        /// <param name="selectedMap"></param>
        internal void OpenRenameMapWindow(MapListItem selectedMap)
        {
            RenameMapViewModel viewModel = new RenameMapViewModel(selectedMap);
            RenameMapWindow window = new RenameMapWindow(viewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            bool? result = window.ShowDialog();

            if (result.GetValueOrDefault(false) == true)
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

        private void menuDeleteSelectedMap_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map to delete first!",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                return;
            }

            MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;

            MessageBoxResult result = System.Windows.MessageBox.Show($"Are you sure you want to delete {selectedItem.DisplayName}?"
                                                                    , "Warning!"
                                                                    , MessageBoxButton.YesNo
                                                                    , MessageBoxImage.Warning
                                                                    , MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                ViewModel.DeleteSelectedMap(selectedItem);
            }
        }

        private void menuSecondMapToLoad_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map first!",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
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
    }
}
