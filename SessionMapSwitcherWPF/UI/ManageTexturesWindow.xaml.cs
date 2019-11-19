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
    /// Interaction logic for ManageTexturesWindow.xaml
    /// </summary>
    public partial class ManageTexturesWindow : Window
    {
        public ManageTexturesViewModel ViewModel { get; set; }

        public ManageTexturesWindow()
        {
            InitializeComponent();

            ViewModel = new ManageTexturesViewModel();
            this.DataContext = ViewModel;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveTexture();
        }

        private void menuRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            RemoveTexture();
        }

        /// <summary>
        /// Validates a item in the list is selected before removing texture
        /// </summary>
        private void RemoveTexture()
        {
            if (lstTextures.SelectedItem == null)
            {
                MessageBox.Show("Select a texture to remove first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ViewModel.RemoveSelectedTexture();
        }
    }
}
