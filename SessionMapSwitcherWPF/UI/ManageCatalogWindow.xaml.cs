using SessionModManagerCore.ViewModels;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

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
            ViewModel.IsAdding = false;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstUrls.SelectedItem == null)
            {
                return;
            }

            ViewModel.RemoveUrl(lstUrls.SelectedItem as CatalogSubscriptionViewModel);
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddUrl(ViewModel.NewUrlText);
            ViewModel.NewUrlText = "";
            ViewModel.IsAdding = false;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAdding = true;
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
    }
}
