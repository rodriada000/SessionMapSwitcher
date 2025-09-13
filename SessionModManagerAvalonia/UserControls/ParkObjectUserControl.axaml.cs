using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SessionModManagerCore.Classes;

namespace SessionModManagerAvalonia;

public partial class ParkObjectUserControl : UserControl
{
    public ParkObjBase ViewModel { get; set; }

    public Image Image { get => objImage; }

    public ParkObjectUserControl()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    public ParkObjectUserControl(ParkObjBase viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Bitmap bitmap = new(ViewModel.ImagePath);

        objImage.Source = bitmap;
    }
}