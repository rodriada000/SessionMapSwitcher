using SessionMapSwitcherCore.Classes;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SessionModManagerWPF.UI
{
    /// <summary>
    /// Interaction logic for GameSettingsUserControl.xaml
    /// </summary>
    public partial class GameSettingsUserControl : UserControl
    {
        private readonly GameSettingsViewModel ViewModel;

        public GameSettingsUserControl()
        {
            InitializeComponent();

            ViewModel = new GameSettingsViewModel();
            this.DataContext = ViewModel;
        }

        private void BtnApplySettings_Click(object sender, RoutedEventArgs e)
        {
            if (UeModUnlocker.IsGamePatched() == false)
            {
                MessageBox.Show("Session has not been patched yet. Click 'Patch With Illusory Mod Unlocker' to patch the game.", "Notice!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ViewModel.UpdateGameSettings();
        }
    }
}
