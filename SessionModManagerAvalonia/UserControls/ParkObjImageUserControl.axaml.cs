using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System.IO;

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
            IsPlayerStart = objData.IsPlayerStart
        };

        DataContext = ViewModel;

        if (File.Exists(ObjectData.ImagePath))
        {
            Bitmap bitmap = new(ObjectData.ImagePath);
            objImage.Source = bitmap.CreateScaledBitmap(scaledSize);
        }
    }
}