using Avalonia.Controls;
using Avalonia.Interactivity;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.ViewModels;
using SessionModManagerCore.Classes;
using MsBox.Avalonia;
using System.Threading.Tasks;
using System;

namespace SessionModManagerAvalonia;

public partial class UpdateWindow : Window
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private UpdateViewModel ViewModel { get; set; }

    public UpdateWindow()
    {
        InitializeComponent();

        ViewModel = new UpdateViewModel();
        this.DataContext = ViewModel;

        if (VersionChecker.LatestRelease != null)
        {
            mdViewer.Markdown = VersionChecker.LatestRelease.VersionNotes;
            ViewModel.HeaderMessage = $"Version {VersionChecker.LatestRelease.Version} of Session Mod Manager is available to download. Release notes:";
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close(false);
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsUpdating = true;
        ViewModel.HeaderMessage = $"Updating Session Mod Manager to v{VersionChecker.LatestRelease?.Version} ...";

        DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged;
        VersionChecker.ExtractProgress = new Progress<double>(percent => ViewModel.UpdatePercent = percent);

        if (OperatingSystem.IsLinux())
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Information", "The app will now close to update. You may need to manually re-open Session Mod Manager after the update finishes.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            var result = await box.ShowAsync();
        }

        BoolWithMessage updateResult = await VersionChecker.UpdateApplication();

        if (updateResult?.Result == false)
        {
            DownloadUtils.ProgressChanged -= DownloadUtils_ProgressChanged;
            ViewModel.IsUpdating = false;
            var box = MessageBoxManager.GetMessageBoxStandard("Error Updating!", updateResult.Message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            var result = await box.ShowAsync();
            this.Close(false);
        }
    }

    private void DownloadUtils_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
    {
        ViewModel.UpdatePercent = progressPercentage ?? 0;
    }

    private void btnUpdateLater_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsUpdating = true;

        AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.UpdateOnExit, true.ToString());
        Task.Factory.StartNew(async () =>
        {
            MessageService.Instance.ShowMessage("New version downloading in background and will be installed on exit.");
            var result = await VersionChecker.PrepareUpdate();
            if (!result.Result)
            {
                MessageService.Instance.ShowMessage(result.Message);
            }
        }); 
        this.Close(false);
    }
}