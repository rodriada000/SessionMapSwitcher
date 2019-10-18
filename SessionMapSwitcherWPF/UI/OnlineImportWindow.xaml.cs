using SessionMapSwitcherCore.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for OnlineImportWindow.xaml
    /// </summary>
    public partial class OnlineImportWindow : Window
    {
        public OnlineImportViewModel ViewModel { get; set; }

        private SynchronizationContext _context;

        public OnlineImportWindow()
        {
            InitializeComponent();

            ViewModel = new OnlineImportViewModel();
            this.DataContext = ViewModel;

            SynchronizationContext existingContext = SynchronizationContext.Current;
            _context = existingContext?.CreateCopy() ?? new SynchronizationContext();
        }

        public OnlineImportWindow(OnlineImportViewModel onlineImportViewModel)
        {
            InitializeComponent();

            this.ViewModel = onlineImportViewModel;
            this.DataContext = ViewModel;

            SynchronizationContext existingContext = SynchronizationContext.Current;
            _context = existingContext?.CreateCopy() ?? new SynchronizationContext();
        }

        private void MenuImportSelected_Click(object sender, RoutedEventArgs e)
        {
            BtnImportSelected_Click(sender, e);
        }

        private void MenuOpenMapUrl_Click(object sender, RoutedEventArgs e)
        {
            BtnPreviewSelected_Click(sender, e);
        }

        private void BtnPreviewSelected_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                MessageBox.Show("Select a map to preview!", "Notice!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DownloadableMap selectedMap = lstMaps.SelectedItem as DownloadableMap;
            selectedMap.OpenPreviewUrlInBrowser();
        }

        private void BtnImportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsImportingMap == false)
            {
                if (lstMaps.SelectedItem == null)
                {
                    MessageBox.Show("Select a map to import!", "Notice!", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DownloadableMap selectedMap = lstMaps.SelectedItem as DownloadableMap;

                ViewModel.IsImportingMap = true;
                ViewModel.MapImported += ViewModel_MapImported;

                ViewModel.ImportSelectedMapAsync(selectedMap);
            }
            else
            {
                ViewModel.CancelPendingImport();
                ViewModel.IsImportingMap = false;
            }
        }

        private void ViewModel_MapImported(bool wasSuccessful)
        {
            _context.Send(ContextCallback, wasSuccessful);
        }

        private void ContextCallback(object entry)
        {
            ViewModel.MapImported -= ViewModel_MapImported;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMapsInBackground();
        }

        private void LoadMapsInBackground()
        {
            ViewModel.HeaderMessage = "Getting list of downloadable maps ...";

            Task downloadTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    // start checking for downloadable maps or load cached list
                    ViewModel.LoadDownloadableMaps();
                }
                catch (Exception ex)
                {
                    ViewModel.HeaderMessage = $"Failed to load list of downloadable maps: {ex.Message}";
                }
            });
        }

        private void MenuDownloadFromBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                MessageBox.Show("Select a map to download!", "Notice!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DownloadableMap selectedMap = lstMaps.SelectedItem as DownloadableMap;
            selectedMap.BeginDownloadInBrowser();
        }

        private void LstMaps_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            menuImportSelected.Header = $"{ViewModel.ImportButtonText} ...";
        }
    }
}
