using SessionMapSwitcher.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        internal void SetSessionPath(string sessionPath)
        {
            ViewModel.PathToSession = sessionPath;
        }
    }
}
