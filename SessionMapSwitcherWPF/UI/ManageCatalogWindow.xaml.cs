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

            ViewModel.RemoveUrl(lstUrls.SelectedItem as string);
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
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
