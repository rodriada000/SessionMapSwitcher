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
using SessionMapSwitcher.ViewModels;

namespace SessionMapSwitcher.UI
{
    /// <summary>
    /// Interaction logic for RenameMapWindow.xaml
    /// </summary>
    public partial class RenameMapWindow : Window
    {
        private RenameMapViewModel ViewModel { get; set; }

        public RenameMapWindow()
        {
            InitializeComponent();

            this.ViewModel = new RenameMapViewModel(null);
            this.DataContext = ViewModel;
        }

        public RenameMapWindow(RenameMapViewModel viewModel)
        {
            InitializeComponent();

            this.ViewModel = viewModel;
            this.DataContext = this.ViewModel;
        }

        private void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ValidateAndSetCustomName())
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void TxtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnRename_Click(sender, e);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtName.SelectAll();
        }
    }
}
