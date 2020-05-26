using SessionModManagerCore.ViewModels;
using System.Windows;
using System.Linq;
using System.Windows.Documents;
using System.Collections.Generic;

namespace SessionModManagerWPF.UI
{
    /// <summary>
    /// Interaction logic for ManageCatalogWindow.xaml
    /// </summary>
    public partial class ManageCatalogWindow : Window
    {
        ManageCatalogViewModel ViewModel { get; set;
        }
        public ManageCatalogWindow()
        {
            InitializeComponent();

            ViewModel = new ManageCatalogViewModel();
            this.DataContext = ViewModel;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsInAddMode = false;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstUrls.SelectedItems?.Count == 0)
            {
                return;
            }

            List<CatalogSubscriptionViewModel> selectedUrls = new List<CatalogSubscriptionViewModel>();

            for (int i = 0; i < lstUrls.SelectedItems.Count; i++)
            {
                selectedUrls.Add(lstUrls.SelectedItems[i] as CatalogSubscriptionViewModel);
            }

            ViewModel.RemoveUrls(selectedUrls);
        }

        private async void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.AddUrl(ViewModel.NewUrlText);
            ViewModel.NewUrlText = "";
            ViewModel.IsInAddMode = false;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsInAddMode = true;
            txtUrl.Focus();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            CreateCatalogWindow createCatalogWindow = new CreateCatalogWindow();
            createCatalogWindow.Show();
        }

        /// <summary>
        /// Save catalog to catalog settings on window close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.TrySaveCatalog();
        }

        private void menuItemActivate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleActivationForAll(true);
        }

        private void menuItemDeactivate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleActivationForAll(false);
        }

        private void menuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            btnRemove_Click(sender, e);
        }

        private async void menuItemAddDefaults_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.AddDefaultCatalogsAsync();
        }
    }
}
