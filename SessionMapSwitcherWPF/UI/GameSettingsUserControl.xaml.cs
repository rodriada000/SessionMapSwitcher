using NLog.Targets;
using NLog;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.IO;

namespace SessionModManagerWPF.UI
{
    /// <summary>
    /// Interaction logic for GameSettingsUserControl.xaml
    /// </summary>
    public partial class GameSettingsUserControl : UserControl
    {
        public readonly GameSettingsViewModel ViewModel;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

        private void btnViewLogs_Click(object sender, RoutedEventArgs e)
        {
            var file = LogManager.Configuration?.AllTargets.OfType<FileTarget>()
                        .Select(x => x.FileName.Render(LogEventInfo.CreateNullEvent()))
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            
            if (!string.IsNullOrWhiteSpace(file))
            {
                file = Path.Combine(SessionPath.ToApplicationRoot, file);

                try
                {
                    Process.Start(new ProcessStartInfo(file) { UseShellExecute = true});
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to open log file");
                }
            }
        }
    }
}
