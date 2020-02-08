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
                    ViewModel.ImportCatalog(fileBrowserDialog.FileName);
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
                    ViewModel.ExportCatalog(fileBrowserDialog.FileName);
                }
            }
        }
    }
}
