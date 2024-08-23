using Avalonia.Controls;
using Avalonia.Interactivity;
using NLog.Targets;
using NLog;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.ViewModels;
using System.Diagnostics;
using System;
using System.Linq;
using MsBox.Avalonia;
using System.IO;
using Avalonia.Styling;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using Avalonia.Controls.ApplicationLifetimes;

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

        string savedTheme = AppSettingsUtil.GetAppSetting(SettingKey.AppTheme);
        var themeOptions = cboTheme?.Items?.Select(i => (i as ComboBoxItem)?.Content).ToList();

        if (!string.IsNullOrWhiteSpace(savedTheme))
        {
            cboTheme.SelectedItem = cboTheme?.Items[themeOptions.IndexOf(savedTheme)];
        }

        if (double.TryParse(AppSettingsUtil.GetAppSetting(SettingKey.FontSize), out double size))
        {
            var sizeOptions = cboFont?.Items?.Select(i => (i as ComboBoxItem)?.Content).ToList();
            if (sizeOptions.IndexOf(size.ToString()) >= 0)
            {
                cboFont.SelectedItem = cboFont?.Items[sizeOptions.IndexOf(size.ToString())];
            }
        }
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

    private void ComboBoxTheme_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        string selectedTheme = (cboTheme?.SelectedValue as ComboBoxItem)?.Content?.ToString();

        if (string.IsNullOrWhiteSpace(selectedTheme))
        {
            return;
        }

        switch(selectedTheme)
        {
            case "System Default":
                App.Current.RequestedThemeVariant = ThemeVariant.Default;
                break;
            case "Light":
                App.Current.RequestedThemeVariant = ThemeVariant.Light;
                break;
            case "Dark":
                App.Current.RequestedThemeVariant = ThemeVariant.Dark;
                break;
        }

        if (selectedTheme != AppSettingsUtil.GetAppSetting(SettingKey.AppTheme))
        {
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AppTheme, selectedTheme);
        }
    }

    private void ComboBoxFont_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        string selectedSize = (cboFont?.SelectedValue as ComboBoxItem)?.Content?.ToString();

        if (string.IsNullOrWhiteSpace(selectedSize))
        {
            return;
        }

        App.Current.Resources["ControlContentThemeFontSize"] = double.Parse(selectedSize);

        if (selectedSize != AppSettingsUtil.GetAppSetting(SettingKey.FontSize))
        {
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.FontSize, selectedSize);
        }
    }


    private async void btnCheckUpdates_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        VersionChecker.CurrentVersion = App.GetAppVersion();

        if (await VersionChecker.IsUpdateAvailable())
        {
            UpdateWindow updateWindow = new UpdateWindow();
            updateWindow.ShowDialog<bool>((Avalonia.Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        }
        else
        {
            MessageService.Instance.ShowMessage("No update available. Latest version already installed.");
        }
    }
}