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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel();
            this.DataContext = ViewModel;
        }

        private void BtnBrowseMapPath_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.SetMapPath(folderBrowserDialog.SelectedPath);
                    ViewModel.LoadAvailableMaps();
                    ViewModel.SetCurrentlyLoadedMap();

                    BackupMapFilesInBackground();
                }
            }
        }

        private void BtnBrowseSessionPath_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderBrowserDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.SetSessionPath(folderBrowserDialog.SelectedPath);
                    ViewModel.SetCurrentlyLoadedMap();

                    if (ViewModel.IsSessionPathValid() == false)
                    {
                        System.Windows.MessageBox.Show("You may have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void BackupMapFilesInBackground()
        {
            ViewModel.UserMessage = "Backing Up Original Session Map ...";
            ViewModel.ButtonsEnabled = false;

            bool didBackup = false;

            Task t = Task.Run(() =>
            {
                didBackup = ViewModel.BackupOriginalMapFiles();
            });

            t.ContinueWith((antecedent) =>
            {
                if (didBackup)
                {
                    ViewModel.UserMessage = "";
                }
                ViewModel.ButtonsEnabled = true;
            });
        }

        private void BtnStartGame_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSessionRunning())
            {
                System.Windows.MessageBox.Show("Session is already running!");
                return;
            }

            if (ViewModel.IsSessionPathValid() == false)
            {
                System.Windows.MessageBox.Show("You have selected an incorrect path to Session. Make sure the directory you choose has the folders 'Engine' and 'SessionGame'.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            System.Diagnostics.Process.Start(ViewModel.PathToSessionExe);
        }

        private void BtnLoadMap_Click(object sender, RoutedEventArgs e)
        {
            if (lstMaps.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Select a map to load first!");
                return;
            }

            if (ViewModel.IsOriginalMapFilesBackedUp() == false)
            {
                System.Windows.MessageBox.Show("The original Session game map files have not been backed up yet. Click OK to backup the files then click 'Load Map' again");
                BackupMapFilesInBackground();
                return;
            }

            MapListItem selectedItem = lstMaps.SelectedItem as MapListItem;

            ViewModel.UserMessage = $"Loading {selectedItem.DisplayName} ...";
            ViewModel.ButtonsEnabled = false;

            Task t = Task.Run(() => ViewModel.LoadMap(selectedItem));

            t.ContinueWith((antecedent) =>
            {
                ViewModel.SetCurrentlyLoadedMap();
                ViewModel.ButtonsEnabled = true;
            });
        }
    }
}
