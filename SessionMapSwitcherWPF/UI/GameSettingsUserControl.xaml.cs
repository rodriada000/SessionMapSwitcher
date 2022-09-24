using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SessionModManagerWPF.UI
{
    /// <summary>
    /// Interaction logic for GameSettingsUserControl.xaml
    /// </summary>
    public partial class GameSettingsUserControl : UserControl
    {
        public readonly GameSettingsViewModel ViewModel;

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
