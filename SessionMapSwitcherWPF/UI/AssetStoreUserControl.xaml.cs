using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.ViewModels;
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

            string header = headerClicked.Column.Header as string;
            Sort(header, direction);

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
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
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
            ViewModel.RemoveSelectedAssetAsync();
        }
    }
}
