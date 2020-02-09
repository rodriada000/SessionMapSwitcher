using SessionMapSwitcherCore.Classes;
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
    /// Interaction logic for CreateCatalogWindow.xaml
    /// </summary>
    public partial class CreateCatalogWindow : Window
    {
        CreateCatalogViewModel ViewModel { get; set; }

        public CreateCatalogWindow()
        {
            InitializeComponent();

            ViewModel = new CreateCatalogViewModel();
            this.DataContext = ViewModel;

            ViewModel.UpdatedAssetInvalid += ViewModel_UpdatedAssetInvalid;
        }

        private void ViewModel_UpdatedAssetInvalid(string validationMessage)
        {
            MessageBox.Show($"The following errors were found:\n\n{validationMessage}", "Failed to Update", MessageBoxButton.OK, MessageBoxImage.Error);
            ViewModel.SelectedAsset = ViewModel.SelectedAsset;
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog fileBrowserDialog = new System.Windows.Forms.OpenFileDialog())
            {
                fileBrowserDialog.Filter = "Catalog Json (*.json)|*.json";
                fileBrowserDialog.Title = "Select Asset Catalog Json File";
                System.Windows.Forms.DialogResult result = fileBrowserDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    BoolWithMessage didImport = ViewModel.ImportCatalog(fileBrowserDialog.FileName);

                    if (!didImport.Result)
                    {
                        MessageBox.Show(didImport.Message, "Failed to Import", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.SaveFileDialog fileBrowserDialog = new System.Windows.Forms.SaveFileDialog())
            {
                fileBrowserDialog.Filter = "Catalog Json (*.json)|*.json";
                fileBrowserDialog.Title = "Save Asset Catalog Json File";
                System.Windows.Forms.DialogResult result = fileBrowserDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    BoolWithMessage didSave = ViewModel.ExportCatalog(fileBrowserDialog.FileName);

                    if (!didSave.Result)
                    {
                        MessageBox.Show(didSave.Message, "Failed to Export", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteAsset(ViewModel.SelectedAsset);
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            BoolWithMessage didAdd = ViewModel.AddAsset();

            if (didAdd.Result)
            {
                lstAssets.ScrollIntoView(ViewModel.SelectedAsset);
            }
            else
            {
                MessageBox.Show(didAdd.Message, "Failed To Add", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.UpdatedAssetInvalid -= ViewModel_UpdatedAssetInvalid;
        }
    }
}
