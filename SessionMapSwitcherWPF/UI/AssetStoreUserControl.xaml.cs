using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.ViewModels;
using SessionModManagerWPF.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SessionModManagerWPF.UI
{
    /// <summary>
    /// Interaction logic for AssetStoreUserControl.xaml
    /// </summary>
    public partial class AssetStoreUserControl : UserControl
    {
        public AssetStoreViewModel ViewModel { get; set; }
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public AssetStoreUserControl()
        {
            InitializeComponent();

            ViewModel = new AssetStoreViewModel();
            this.DataContext = ViewModel;
        }


        /// <summary>
        /// Sorts the ListView based on the clicked column
        /// </summary>
        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked == null || headerClicked?.Role == GridViewColumnHeaderRole.Padding)
            {
                return;
            }


            if (headerClicked != _lastHeaderClicked)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                if (_lastDirection == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }
            }

            Binding headerBinding = headerClicked.Column.DisplayMemberBinding.ProvideValue(null) as Binding;

            if (headerBinding == null)
            {
                return;
            }

            string propertyNameToSortBy = headerBinding.Path?.Path;
            Sort(propertyNameToSortBy, direction);

            _lastHeaderClicked = headerClicked;
            _lastDirection = direction;
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(lstAssets.ItemsSource);

            if (dataView == null)
            {
                return;
            }

            dataView.SortDescriptions.Clear();
            (dataView as ListCollectionView).CustomSort = null;

            if (sortBy == nameof(AssetViewModel.UpdatedDate))
            {
                DateTimeComparer sorter = new DateTimeComparer()
                {
                    SortDirection = direction
                };
                (dataView as ListCollectionView).CustomSort = sorter;
            }
            else
            {
                SortDescription sd = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();
            }
        }

        private void lstAssets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstAssets.SelectedItem == null)
            {
                return;
            }

            ViewModel.RefreshPreviewForSelected();
        }
        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAsset == null)
            {
                MessageBox.Show("Select an asset to install first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ViewModel.DownloadSelectedAssetAsync();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveSelectedAsset();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CheckForCatalogUpdatesAsync();
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            OpenUploadAssetForm();
        }

        private void OpenUploadAssetForm()
        {
            UploadAssetViewModel viewModel = new UploadAssetViewModel()
            {
                AvailableBuckets = new List<string>()
            };

            UploadAssetWindow window = new UploadAssetWindow(viewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            bool? result = window.ShowDialog();

            ViewModel.UserMessage = "Force refresh list of assets to view uploaded asset.";
        }

        private void menuItemCancelDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadItemViewModel downloadItem = lstDownloads.SelectedItem as DownloadItemViewModel;

            if (downloadItem == null)
            {
                return;
            }

            downloadItem.OnCancel?.Invoke();
        }

        private void btnManageCat_Click(object sender, RoutedEventArgs e)
        {
            ManageCatalogWindow catalogWindow = new ManageCatalogWindow();
            catalogWindow.ShowDialog();
            ViewModel.CheckForCatalogUpdatesAsync();
        }
    }
}
