using SessionMapSwitcher.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using SessionMapSwitcher.Classes;
using SessionMapSwitcher.Utils;
using SessionMapSwitcher.Classes.Events;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel ViewModel;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel();
            ReloadAvailableMapsInBackground();

            this.DataContext = ViewModel;
            this.Title = $"{App.GetAppName()} - v{App.GetAppVersion()}";
        }

        private void BtnBrowseSessionPath_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    SetAndValidateSessionPath(folderBrowserDialog.SelectedPath);
                }
            }
        }

        private void BackupMapFilesInBackground()
        {
            ViewModel.UserMessage = "Backing Up Original Session Map ...";
            ViewModel.InputControlsEnabled = false;

            bool didBackup = false;

            Task t = Task.Run(() =>
            {
                didBackup = ViewModel.BackupOriginalMapFiles();
            });

            t.ContinueWith((antecedent) =>
            {
                if (didBackup)
                {
                    ViewModel.UserMessage = "";
                }
                ViewModel.InputControlsEnabled = true;
            });
        }

        private void BtnStartGame_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSessionRunning())
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("Session is already running! Click 'Yes' if you want to restart the game.", "Notice!", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    // kill the process
                    System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcessesByName("SessionGame-Win64-Shipping");
                    if (procs.Length > 0)
                    {
                        procs[0].Kill();
                    }
                }
            }

            if (SessionPath.IsSessionPathValid() == false)
            {
                System.Windows.MessageBox.Show("You have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (UnpackUtils.IsSessionUnpacked() == false)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("It seems the Session game has not been unpacked. This is required before using Map Switcher.\n\nWould you like to download the required files to auto-unpack?", 
                                                "Notice!", 
                                                MessageBoxButton.YesNo, 
                                                MessageBoxImage.Warning, 
                                                MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    BeginUnpackingProcess();
                }

                return;
            }

            if (UnpackUtils.IsSessionPakFileRenamed() == false)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("It seems the .pak file has not been renamed yet. This is required before using custom maps and the Map Switcher.\n\nClick 'Yes' to auto rename the .pak file.",
                                                "Notice!",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Information,
                                                MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    bool didRename = UnpackUtils.RenamePakFile();

                    if (didRename == false)
                    {
                        System.Windows.MessageBox.Show("The .pak file could not be renamed. Make sure the game is unpacked correctly and try again.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // validate and set game settings
            bool didSet = ViewModel.WriteGameSettingsToFile();

            if (didSet == false)
            {
                ViewModel.UserMessage = "Cannot start game: " + ViewModel.UserMessage;
                return; // do not start game with invalid settings
            }

            LoadMapInBackgroundAndContinueWith((antecedent) =>
            {
                System.Diagnostics.Process.Start(SessionPath.ToSessionExe);
                ViewModel.InputControlsEnabled = true;
            });
        }

        private void BeginUnpackingProcess()
        {
            ViewModel.StartUnpacking();
        }

        private void BtnLoadMap_Click(object sender, RoutedEventArgs e)
        {
            // double check the controls are disabled and should not load (e.g. when double clicking map in list)
            if (ViewModel.InputControlsEnabled == false)
            {
                return;
            }

            LoadMapInBackgroundAndContinueWith((antecedent) =>
            {
                ViewModel.InputControlsEnabled = true;
            });
        }

        private void LoadMapInBackgroundAndContinueWith(Action<Task> continuationTask)
        {
            if (UnpackUtils.IsSessionUnpacked() == false)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("It seems the Session game has not been unpacked. This is required before using Map Switcher.\n\nWould you like to download the required files to auto-unpack?",
                                                "Notice!",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Warning,
                                                MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    BeginUnpackingProcess();
                }

                return;
            }

            if (ViewModel.IsOriginalMapFileBackedUp() == false)
            {
                System.Windows.MessageBox.Show("The original Session game map files have not been backed up yet. Click OK to backup the files then click 'Load Map' again",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                BackupMapFilesInBackground();
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

            ViewModel.UserMessage = $"Loading {selectedItem.MapName} ...";
            ViewModel.InputControlsEnabled = false;

            Task t = Task.Run(() => ViewModel.LoadMap(selectedItem));

            t.ContinueWith(continuationTask);
        }

        private void BtnReloadMaps_Click(object sender, RoutedEventArgs e)
        {
            ReloadAvailableMapsInBackground();
        }

        private void ReloadAvailableMapsInBackground()
        {
            ViewModel.UserMessage = $"Reloading Available Maps ...";
            ViewModel.InputControlsEnabled = false;

            Task t = Task.Factory.StartNew(() => ViewModel.LoadAvailableMaps(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.FromCurrentSynchronizationContext());

            t.ContinueWith((antecedent) =>
            {
                ViewModel.InputControlsEnabled = true;
            });
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
                ViewModel.UserMessage = "Session Path updated!";
            }
        }

        private void SetAndValidateSessionPath(string path)
        {
            ViewModel.SetSessionPath(path); // this will save it to app settings
            ViewModel.SetCurrentlyLoadedMap();

            if (SessionPath.IsSessionPathValid())
            {
                ViewModel.RefreshGameSettingsFromIniFiles();
                ViewModel.GetObjectCountFromFile();
                ReloadAvailableMapsInBackground();
                BackupMapFilesInBackground();
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
                ViewModel.UserMessage = "Cannot open folder: No map selected.";
                return;
            }

            MapListItem selectedMap = lstMaps.SelectedItem as MapListItem;
            ViewModel.OpenFolderToSelectedMap(selectedMap);
        }

        private void ChkShowInvalidMaps_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.FilteredAvailableMaps.Refresh();
        }

        private void BtnApplySettings_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.InputControlsEnabled == false)
            {
                return;
            }

            ViewModel.InputControlsEnabled = false;

            bool didSet = ViewModel.WriteGameSettingsToFile();

            ViewModel.InputControlsEnabled = true;

            if (didSet)
            {
                ViewModel.UserMessage = "Game settings updated!";

                if (ViewModel.IsSessionRunning())
                {
                    ViewModel.UserMessage += " Restart the game for changes to take effect.";
                }
            }
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
            importContextMenu.IsOpen = true;
        }

        private void MenuComputerImport_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenComputerImportWindow();
        }

        private void MenuOnlineImport_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenOnlineImportWindow();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckForNewVersionInBackground();
            ctrlTextureReplacer.ViewModel.MessageChanged += TextureReplacer_MessageChanged;
            ctrlProjectWatcher.ViewModel.MapImported += ProjectWatcher_MapImported;
        }

        private void TextureReplacer_MessageChanged(string message)
        {
            ViewModel.UserMessage = message;
        }

        private void ProjectWatcher_MapImported(object sender, MapImportedEventArgs e)
        {
            ViewModel.LoadMap(e.MapName);
        }

        private void CheckForNewVersionInBackground()
        {
            bool isNewVersionAvailable = false;

            ViewModel.UserMessage = "Checking for updates ...";
            Task task = Task.Factory.StartNew(() => 
            {
                isNewVersionAvailable = VersionChecker.CheckForUpdates();
            });

            task.ContinueWith((antecedent) =>
            {
                ViewModel.UserMessage = "";

                if (isNewVersionAvailable)
                {
                    MessageBoxResult result = System.Windows.MessageBox.Show("There is a new version available. Click 'Yes' to download the latest version (the program will close and re-open).", 
                                                   "Update Available!",
                                                   MessageBoxButton.YesNo,
                                                   MessageBoxImage.Question,
                                                   MessageBoxResult.Yes);

                    if (result == MessageBoxResult.Yes)
                    {
                        ViewModel.UserMessage = "Updating app ...";
                        VersionChecker.AppUpdater.ReportProgress += AppUpdater_ReportProgress;

                        Task updateTask = Task.Factory.StartNew(() =>
                        {
                            VersionChecker.UpdateApplication();
                        });

                        updateTask.ContinueWith((updateAntecedent) => 
                        {
                            VersionChecker.AppUpdater.ReportProgress -= AppUpdater_ReportProgress;
                        });
                    }
                }
            });
        }

        private void AppUpdater_ReportProgress(NAppUpdate.Framework.Common.UpdateProgressInfo currentStatus)
        {
            ViewModel.UserMessage = $"Updating app: {currentStatus.Message} | {currentStatus.Percentage}%";
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
                bool hasImportLocation = MetaDataManager.IsImportLocationStored(selected.MapName);
                menuReimporSelectedMap.IsEnabled = hasImportLocation;
                menuReimporSelectedMap.ToolTip = hasImportLocation ? null : "You can only re-import if you imported the map from 'Import Map > From Computer ...' and imported a folder.\n(does not work with .zip files)";
                menuHideSelectedMap.Header = selected.IsHiddenByUser ? "Show Selected Map ..." : "Hide Selected Map ...";
            }
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            ctrlTextureReplacer.ViewModel.MessageChanged -= TextureReplacer_MessageChanged;
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
            ViewModel.OpenRenameMapWindow(selectedItem);
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
    }
}
