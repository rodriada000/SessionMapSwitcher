using System.Windows;
using System.Windows.Input;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.ViewModels;

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
            BoolWithMessage renameResult = ViewModel.ValidateAndSetCustomName();
            if (renameResult.Result)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(renameResult.Message, "Error Renaming!", MessageBoxButton.OK, MessageBoxImage.Error);
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
