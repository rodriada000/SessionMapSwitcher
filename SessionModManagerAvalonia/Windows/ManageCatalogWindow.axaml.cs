using Avalonia.Controls;
using Avalonia.Interactivity;
using SessionModManagerCore.ViewModels;
using System.Collections.Generic;

namespace SessionModManagerAvalonia;

public partial class ManageCatalogWindow : Window
{
    ManageCatalogViewModel ViewModel
    {
        get; set;
    }
    public ManageCatalogWindow()
    {
        InitializeComponent();

        ViewModel = new ManageCatalogViewModel();
        this.DataContext = ViewModel;
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsInAddMode = false;
    }

    private void btnRemove_Click(object sender, RoutedEventArgs e)
    {
        if (lstUrls.SelectedItems?.Count == 0)
        {
            return;
        }

        List<CatalogSubscriptionViewModel> selectedUrls = new List<CatalogSubscriptionViewModel>();

        for (int i = 0; i < lstUrls.SelectedItems.Count; i++)
        {
            selectedUrls.Add(lstUrls.SelectedItems[i] as CatalogSubscriptionViewModel);
        }

        ViewModel.RemoveUrls(selectedUrls);
    }

    private async void btnConfirm_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddUrl(ViewModel.NewUrlText);
        ViewModel.NewUrlText = "";
        ViewModel.IsInAddMode = false;
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsInAddMode = true;
        txtUrl.Focus();
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void btnCreate_Click(object sender, RoutedEventArgs e)
    {
        CreateCatalogWindow createCatalogWindow = new CreateCatalogWindow();
        createCatalogWindow.Show();
    }


    private void menuItemActivate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleActivationForAll(true);
    }

    private void menuItemDeactivate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleActivationForAll(false);
    }

    private void menuItemRemove_Click(object sender, RoutedEventArgs e)
    {
        btnRemove_Click(sender, e);
    }

    private async void menuItemAddDefaults_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddDefaultCatalogsAsync();
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        ViewModel.TrySaveCatalog();
    }
}