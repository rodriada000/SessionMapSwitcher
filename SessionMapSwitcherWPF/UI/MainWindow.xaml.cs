using System;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcher.UI;
using System.Windows.Threading;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherWPF.Classes;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel ViewModel;

        private bool IsNewVersionAvailable = false;

        /// <summary>
        /// used to prevent the window size to be saved to app config when setting it
        /// </summary>
        private bool IsSettingWindowSize = false;

        /// <summary>
        /// Timer to trigger the update window to open after a second
        /// if there is an update avaialble. This is used so the Window
        /// is created on the main UI thread
        /// </summary>
        private DispatcherTimer updateTimer;

        private int SelectedTabIndex;

        public MainWindow()
        {
            InitializeComponent();

            SetCustomWindowSizeFromAppSettings();

            ViewModel = new MainWindowViewModel();

            MessageService.Instance.MessageReceived += MessageService_MessageReceived;

            this.DataContext = ViewModel;
            this.Title = $"{App.GetAppName()} - v{App.GetAppVersion()}";

            SelectedTabIndex = tabControl.SelectedIndex;

            // set window state
            string windowStateVal = AppSettingsUtil.GetAppSetting(SettingKey.WindowState);
            if (!string.IsNullOrWhiteSpace(windowStateVal) && WindowState.TryParse(windowStateVal, out WindowState state))
            {
                this.WindowState = state;
            }
        }

        private void MessageService_MessageReceived(string message)
        {
            ViewModel.UserMessage = message;
        }

        private void SetCustomWindowSizeFromAppSettings()
        {
            IsSettingWindowSize = true;

            string customSize = AppSettingsUtil.GetAppSetting(SettingKey.CustomWindowSize);

            if (string.IsNullOrEmpty(customSize))
            {
                IsSettingWindowSize = false;
                return;
            }

            string[] dimensions = customSize.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);


            bool didParseWidth = double.TryParse(dimensions[0], out double newWidth);
            bool didParseHeight = double.TryParse(dimensions[1], out double newHeight);

            if (didParseWidth && didParseHeight)
            {
                this.Width = newWidth;
                this.Height = newHeight;
            }


            IsSettingWindowSize = false;
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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckForNewVersionInBackground();
            controlTextureMan.ViewModel.MessageChanged += MessageService_MessageReceived;
            //ctrlProjectWatcher.ViewModel.MapImported += ProjectWatcher_MapImported;
        }

        //private void ProjectWatcher_MapImported(object sender, MapImportedEventArgs e)
        //{
        //    controlMapSelection.ViewModel.LoadMap(e.MapName);
        //}

        #region Update Related Methods

        private void CheckForNewVersionInBackground()
        {
            ViewModel.UserMessage = "Checking for updates ...";
            Task task = Task.Factory.StartNew(() =>
            {
                IsNewVersionAvailable = VersionChecker.IsUpdateAvailable();
            });


            task.ContinueWith((antecedent) =>
            {
                ViewModel.UserMessage = "";

                if (IsNewVersionAvailable)
                {
                    StartUpdateTimerToOpenWindow();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Creates and starts a timer to open the <see cref="UpdateWindow"/> after a few milliseconds
        /// </summary>
        private void StartUpdateTimerToOpenWindow()
        {
            updateTimer = new DispatcherTimer();
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Interval = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 250);
            updateTimer.Start();
        }

        /// <summary>
        /// Stops the timer and shows the <see cref="UpdateWindow"/>
        /// </summary>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Tick -= UpdateTimer_Tick;
            }

            UpdateWindow updateWindow = new UpdateWindow();
            updateWindow.ShowDialog();
        }

        #endregion


        private void mainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsSettingWindowSize || IsLoaded == false)
            {
                return;
            }

            string newSize = $"{this.ActualWidth},{this.ActualHeight}";
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.CustomWindowSize, newSize);
        }


        private void tabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedIndex != SelectedTabIndex)
            {
                SelectedTabIndex = tabControl.SelectedIndex;

                if (tabMainWindow.IsSelected && controlAssetStore.ViewModel.HasDownloadedMap)
                {
                    controlAssetStore.ViewModel.HasDownloadedMap = false;
                    controlMapSelection.ViewModel.ReloadAvailableMapsInBackground();
                }
                else if (tabTextureManager.IsSelected)
                {
                    controlTextureMan.ViewModel.InitInstalledTextures();
                }
            }

        }

        private void BtnPatch_Click(object sender, RoutedEventArgs e)
        {
            PromptToPatch();
        }

        /// <summary>
        /// Prompts to download Illusory mod unlocker or opens it if already installed
        /// </summary>
        public void PromptToPatch()
        {
            string displayName = "IllusoryUniversalModUnlocker";

            if (!RegistryHelper.IsSoftwareInstalled(displayName, Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry32))
            {
                ModUnlockerDownloadLink downloadInfo = UeModUnlocker.GetLatestDownloadLinkInfo();

                if (downloadInfo == null || string.IsNullOrEmpty(downloadInfo.Url))
                {
                    System.Windows.MessageBox.Show("Failed to get the latest download link. Check that you have internet and try again.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBoxResult result = System.Windows.MessageBox.Show("This will open your browser to download the Illusory Universal Mod Unlocker.\n\nLaunch the unlocker installer after it downloads to patch Session.\n\nDo you want to continue?",
                                                                         "Notice!",
                                                                         MessageBoxButton.YesNo,
                                                                         MessageBoxImage.Warning,
                                                                         MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(downloadInfo.Url));
                }
            }
            else
            {
                string modUnlockerPath = RegistryHelper.GetExePathFromDisplayIcon(displayName, Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry32);
                Process.Start(modUnlockerPath);
            }
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controlTextureMan.ViewModel.MessageChanged -= MessageService_MessageReceived;
            MessageService.Instance.MessageReceived -= MessageService_MessageReceived;
            ImageCache.WriteToFile();
        }

        private void mainWindow_StateChanged(object sender, EventArgs e)
        {
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.WindowState, this.WindowState.ToString());
        }
    }
}
