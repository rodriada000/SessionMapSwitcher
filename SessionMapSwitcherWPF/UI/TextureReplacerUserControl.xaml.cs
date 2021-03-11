using SessionModManagerCore.ViewModels;
using SessionModManagerWPF.UI;
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
            ViewModel.InitInstalledTextures();

            this.DataContext = ViewModel;
        }

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ReplaceTextures();
            ViewModel.InitInstalledTextures();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFiles();
        }

        internal void BrowseForFiles()
        {
            using (System.Windows.Forms.OpenFileDialog fileBrowserDialog = new System.Windows.Forms.OpenFileDialog())
            {
                fileBrowserDialog.Filter = "*.uasset *.zip *.rar file|*.uasset;*.zip;*.rar|All files (*.*)|*.*";
                fileBrowserDialog.Title = "Select .uasset Texture File, .zip, or .rar File Containing Texture Files";
                System.Windows.Forms.DialogResult result = fileBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.PathToFile = fileBrowserDialog.FileName;
                }
            }
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

        /// <summary>
        /// Validates a item in the list is selected before removing texture
        /// </summary>
        private void RemoveTexture()
        {
            if (lstTextures.SelectedItem == null)
            {
                MessageBox.Show("Select a mod to remove first.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ViewModel.RemoveSelectedTexture();
        }
    }
}
