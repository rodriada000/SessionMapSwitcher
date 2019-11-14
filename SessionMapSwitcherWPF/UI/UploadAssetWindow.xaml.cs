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
    /// Interaction logic for UploadAssetWindow.xaml
    /// </summary>
    public partial class UploadAssetWindow : Window
    {
        public UploadAssetViewModel ViewModel { get; set; }

        public UploadAssetWindow()
        {
            InitializeComponent();

            ViewModel = new UploadAssetViewModel();
            this.DataContext = ViewModel;
        }

        public UploadAssetWindow(UploadAssetViewModel viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;
            this.DataContext = ViewModel;
        }

        private void btnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFile();
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UploadAsset();
        }

        private void btnBrowseThumbnail_Click(object sender, RoutedEventArgs e)
        {
            BrowseForThumbnailFile();
        }

        internal void BrowseForFile()
        {
            using (System.Windows.Forms.OpenFileDialog fileBrowserDialog = new System.Windows.Forms.OpenFileDialog())
            {
                fileBrowserDialog.Filter = "Zip file (*.zip)|*.zip|Rar file (*.rar)|*.rar";
                fileBrowserDialog.Title = "Select .zip or .rar File Containing Asset To Upload";
                System.Windows.Forms.DialogResult result = fileBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.PathToFile = fileBrowserDialog.FileName;
                }
            }
        }

        internal void BrowseForThumbnailFile()
        {
            using (System.Windows.Forms.OpenFileDialog fileBrowserDialog = new System.Windows.Forms.OpenFileDialog())
            {
                fileBrowserDialog.Filter = "png file (*.png)|*.png|jpg file (*.jpg)|*.jpg|bmp file (*.bmp)|*.bmp";
                fileBrowserDialog.Title = "Select thumbnail image for asset";
                System.Windows.Forms.DialogResult result = fileBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.PathToThumbnail = fileBrowserDialog.FileName;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AuthenticateOrPromptUserToAuthenticate();
        }

        /// <summary>
        /// Try to authenticate with provided credentials. If authentication fails or
        /// credentials not provided then ask user to set the .json file to use
        /// </summary>
        private void AuthenticateOrPromptUserToAuthenticate()
        {
            string messageToUser = "Unable to authenticate with the provided upload credentials.";


            if (String.IsNullOrEmpty(ViewModel.PathToCredentialsJson))
            {
                // credentials have not been set so don't try to authenticate yet
                messageToUser = "The upload credentials have not been set yet.";
            }
            else
            {
                // try to authenticate on load then prompt user to set credentials if not authenticated
                ViewModel.TryAuthenticate();
            }


            if (ViewModel.HasAuthenticated)
            {
                ViewModel.StatusMessage = UploadAssetViewModel.DefaultStatusMesssage;
                ViewModel.SetBucketBasedOnAuthor();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show($"{messageToUser}\n\nDo you want to set the credentials .json file?", "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (System.Windows.Forms.OpenFileDialog fileBrowserDialog = new System.Windows.Forms.OpenFileDialog())
                    {
                        fileBrowserDialog.Filter = "json file (*.json)|*.json";
                        fileBrowserDialog.Title = "Select .json File With Credentials";
                        System.Windows.Forms.DialogResult browseResult = fileBrowserDialog.ShowDialog();

                        if (browseResult == System.Windows.Forms.DialogResult.OK)
                        {
                            ViewModel.SetPathToCredentialsJson(fileBrowserDialog.FileName);
                            AuthenticateOrPromptUserToAuthenticate(); // try to authenticate now that path is set
                        }
                    }
                }
            }
        }
    }
}
