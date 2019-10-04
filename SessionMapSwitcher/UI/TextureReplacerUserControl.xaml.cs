using SessionMapSwitcher.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SessionMapSwitcher.UI
{
    /// <summary>
    /// Interaction logic for TextureReplacerUserControl.xaml
    /// </summary>
    public partial class TextureReplacerUserControl : UserControl
    {
        public TextureReplacerViewModel ViewModel { get; set; }

        public TextureReplacerUserControl()
        {
            InitializeComponent();

            ViewModel = new TextureReplacerViewModel();
            this.DataContext = ViewModel;
        }

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ReplaceTextures();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.BrowseForFiles();
        }
    }
}
