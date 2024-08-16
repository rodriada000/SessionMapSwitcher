using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.ViewModels;

namespace SessionModManagerAvalonia;

public partial class RenameMapWindow : Window
{
    private RenameMapViewModel ViewModel { get; set; }

    public RenameMapWindow()
    {
        InitializeComponent();

        this.ViewModel = new RenameMapViewModel(null);
        this.DataContext = ViewModel;
    }

    public RenameMapWindow(RenameMapViewModel viewModel)
    {
        InitializeComponent();

        this.ViewModel = viewModel;
        this.DataContext = this.ViewModel;
    }

    private async void BtnRename_Click(object sender, RoutedEventArgs e)
    {
        BoolWithMessage renameResult = ViewModel.ValidateAndSetCustomName();
        if (renameResult.Result)
        {
            this.Close(true);
        }
        else
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error Renaming!", renameResult.Message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            var result = await box.ShowAsync();
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtName.SelectAll();
    }

    private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnRename_Click(sender, e);
        }
    }
}