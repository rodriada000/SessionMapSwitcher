using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System.IO;

namespace SessionModManagerAvalonia;

public partial class ParkObjectUserControl : UserControl
{
    public ParkCatalogObjViewModel ViewModel { get; set; }

    public Image Image { get => objImage; }

    public ParkObjectUserControl()
    {
        InitializeComponent();
        ViewModel = new ParkCatalogObjViewModel();
        ViewModel.ObjectData = new ParkObjBase();
        DataContext = ViewModel;
    }

    public ParkObjectUserControl(ParkObjBase parkObj)
    {
        InitializeComponent();
        ViewModel = new ParkCatalogObjViewModel { ObjectData = parkObj, Name = parkObj.Name, IsSelected = parkObj.IsSelected };
        DataContext = ViewModel;

        if (File.Exists(ViewModel.ObjectData.ImagePath))
        {
            Bitmap bitmap = new(ViewModel.ObjectData.ImagePath);
            objImage.Source = bitmap;
        }
    }
}