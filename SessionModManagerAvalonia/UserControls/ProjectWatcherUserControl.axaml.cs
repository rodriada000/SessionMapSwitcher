using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using Avalonia.Input;
using MsBox.Avalonia;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using SessionMapSwitcherCore.Utils;
using SessionModManagerAvalonia.Windows;
using System.Collections.Generic;

namespace SessionModManagerAvalonia;

public partial class ProjectWatcherUserControl : UserControl
{
    public ProjectWatcherViewModel ViewModel { get; set; }

    public ProjectWatcherUserControl()
    {
        InitializeComponent();

        ViewModel = new ProjectWatcherViewModel();
        this.DataContext = ViewModel;
    }

    private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        await BrowseForProject();
    }

    private void BtnWatch_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.WatchProject();
    }

    private void BtnUnwatch_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.UnwatchProject();
    }

    internal async Task BrowseForProject()
    {
        ViewModel.UnwatchProject();

        var topLevel = TopLevel.GetTopLevel(this);

        IReadOnlyList<FilePickerFileType> filters = new List<FilePickerFileType>() { new FilePickerFileType("Unreal Engine Projects (*.uproject)") { Patterns = new List<string>() { "*.uproject" } } };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select .uproject File",
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        if (files.Any())
        {
            ViewModel.PathToProject = files.FirstOrDefault().TryGetLocalPath();
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.ProjectWatcherPath, ViewModel.PathToProject);
        }

        ViewModel.StatusText = "Not watching project.";
    }
}