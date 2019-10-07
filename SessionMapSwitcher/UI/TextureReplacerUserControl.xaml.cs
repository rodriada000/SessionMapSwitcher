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


        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void TextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length != 0)
            {
                ViewModel.PathToFile = files[0];
                if (ViewModel.PathToFile.EndsWith(".uexp") || ViewModel.PathToFile.EndsWith(".ubulk"))
                {
                    // fix file extension since user dropped different file type in but assume the .uasset file also exists
                    ViewModel.PathToFile = ViewModel.PathToFile.Replace(".uexp", ".uasset").Replace(".ubulk", ".uasset");
                }
            }
        }
    }
}
