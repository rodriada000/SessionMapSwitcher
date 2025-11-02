using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;

namespace SessionModManagerAvalonia;

public partial class ParkObjImageUserControl : UserControl
{
    public ParkObjBase ObjectData { get => ViewModel.ObjectData; }
    public ParkObjViewModel ViewModel { get; set; }

    public Image Image { get => objImage; }

    public ParkObjImageUserControl()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    public ParkObjImageUserControl(ParkObjBase objData, PixelSize scaledSize)
    {
        InitializeComponent();
        ViewModel = new ParkObjViewModel()
        {
            ObjectData = objData,
        };

        DataContext = ViewModel;
        Bitmap bitmap = new(ObjectData.ImagePath);
        objImage.Source = bitmap.CreateScaledBitmap(scaledSize);
    }
}