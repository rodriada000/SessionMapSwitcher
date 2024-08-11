using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NLog.Targets;
using NLog;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.ViewModels;
using System.Diagnostics;
using System;
using System.Linq;
using MsBox.Avalonia;
using System.IO;

namespace SessionModManagerAvalonia;

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

    private async void BtnApplySettings_Click(object sender, RoutedEventArgs e)
    {
        if (UeModUnlocker.IsGamePatched() == false)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice!", "Session has not been patched yet. Click 'Patch With Illusory Mod Unlocker' to patch the game.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            var result = await box.ShowAsync();
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

            if (File.Exists(file))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to open log file");
                }
            }
        }
    }
}