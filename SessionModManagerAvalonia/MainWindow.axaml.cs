using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using SessionMapSwitcher.Classes.Events;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Threading.Tasks;

namespace SessionModManagerAvalonia
{
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

        private void btnPatch_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void TabControl_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            if (tabControl == null)
                return;

            if (tabControl.SelectedIndex != SelectedTabIndex)
            {
                SelectedTabIndex = tabControl.SelectedIndex;

                if (tabMainWindow.IsSelected)
                {
                    controlMapSelection.ViewModel.CheckForRMSTools(); // ensures the rms tools is enabled when switching tabs (in the case the user installs the mod then switches back to the map selection)

                    if (controlAssetStore.ViewModel.HasDownloadedMap)
                    {
                        controlAssetStore.ViewModel.HasDownloadedMap = false;
                        controlMapSelection.ViewModel.ReloadAvailableMapsInBackground();
                    }
                }
                else if (tabTextureManager.IsSelected)
                {
                    controlTextureMan.ViewModel.LoadInstalledTextures();
                }
                else if (tabSettings.IsSelected)
                {
                    controlSettings.ViewModel.RefreshGameSettings();
                }
            }
        }

        private void Window_Loaded_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CheckForNewVersionInBackground();
            controlTextureMan.ViewModel.MessageChanged += MessageService_MessageReceived;
            controlSettings.ctrlProjectWatcher.ViewModel.MapImported += ProjectWatcher_MapImported;
        }

        private void ProjectWatcher_MapImported(object sender, MapImportedEventArgs e)
        {
            controlMapSelection.ViewModel.LoadMap(e.MapName);
        }

        private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
        {
            controlTextureMan.ViewModel.MessageChanged -= MessageService_MessageReceived;
            MessageService.Instance.MessageReceived -= MessageService_MessageReceived;
            ImageCache.WriteToFile();
        }

        private void Window_SizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
        {
            if (IsSettingWindowSize || IsLoaded == false)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            string newSize = $"{topLevel?.Width},{topLevel?.Height}";
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.CustomWindowSize, newSize);
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

                if (IsNewVersionAvailable)
                {
                    ViewModel.UserMessage = "New update available to download";
                    StartUpdateTimerToOpenWindow();
                } else
                {
                    ViewModel.UserMessage = "";
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
            updateTimer.Interval = new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 1);
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
            updateWindow.ShowDialog<bool>((Avalonia.Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        }

        #endregion

    }
}