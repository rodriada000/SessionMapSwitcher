using SessionMapSwitcher.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel();
            ReloadAvailableMapsInBackground();

            this.DataContext = ViewModel;
        }

        private void BtnBrowseMapPath_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.SetMapPath(folderBrowserDialog.SelectedPath);
                    ReloadAvailableMapsInBackground();

                    BackupMapFilesInBackground();
                }
            }
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
                System.Windows.MessageBox.Show("Session is already running!");
                return;
            }

            if (ViewModel.IsSessionPathValid() == false)
            {
                System.Windows.MessageBox.Show("You have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadMapInBackgroundAndContinueWith((antecedent) =>
            {
                ViewModel.InputControlsEnabled = true;
                System.Diagnostics.Process.Start(ViewModel.PathToSessionExe);
            });
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
            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map to load first!",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                return;
            }

            if (ViewModel.IsOriginalMapFilesBackedUp() == false)
            {
                System.Windows.MessageBox.Show("The original Session game map files have not been backed up yet. Click OK to backup the files then click 'Load Map' again",
                                                "Notice!",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Information);
                BackupMapFilesInBackground();
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

            ViewModel.UserMessage = $"Loading {selectedItem.DisplayName} ...";
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
                SetAndValidateSessionPath(ViewModel.SessionPath); // the viewmodel is 2-way binded so the new path value is already set when enter is pressed so we pass the same value to store in app setttings and validate it
                ViewModel.UserMessage = "Session Path updated!";
            }
        }

        private void SetAndValidateSessionPath(string path)
        {
            ViewModel.SetSessionPath(path); // this will save it to app settings
            ViewModel.SetCurrentlyLoadedMap();

            if (ViewModel.IsSessionPathValid() == false)
            {
                System.Windows.MessageBox.Show("You may have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TxtMapPath_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.SetMapPath(ViewModel.MapPath);

                ViewModel.UserMessage = $"Reloading Available Maps ...";
                ViewModel.InputControlsEnabled = false;

                bool didReload = false;

                Task t = Task.Factory.StartNew(() =>
                {
                    didReload = ViewModel.LoadAvailableMaps();
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.FromCurrentSynchronizationContext());

                t.ContinueWith((antecedent) =>
                {
                    ViewModel.InputControlsEnabled = true;
                    if (didReload)
                    {
                        ViewModel.UserMessage = "Map Path updated! The original game files may have to be backed up to this path before starting the game.";
                    }
                });

            }
        }

        private void MenuOpenSessionFolder_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenFolderToSession();
        }

        private void MenuOpenMapsFolder_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenFolderToAvailableMaps();
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
    }
}
