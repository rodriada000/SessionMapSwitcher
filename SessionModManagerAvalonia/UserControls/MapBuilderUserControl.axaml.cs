using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System.Diagnostics;

namespace SessionModManagerAvalonia;

public partial class MapBuilderUserControl : UserControl
{
    bool _dragging = false;
    UAssetEditor _assetEditor = new UAssetEditor();
    CanvasViewModel _canvas = new CanvasViewModel();
    Image? _lastSelected;

    public MapBuilderUserControl()
    {
        InitializeComponent();
    }

    private void Rectangle_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        string b;
        _dragging = true;
        _lastSelected = (Image)sender;
    }

    private void Rectangle_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _dragging = false;
    }

    private void Rectangle_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_dragging)
        {
            Point value = e.GetPosition(this.canvasMap);

            if (value.X >= 0 && value.X <= this.canvasMap.Width && value.X % 10 == 0)
            {
                Canvas.SetLeft((Image)sender, value.X);
                ((ParkObjBase)_lastSelected.DataContext).Position.X = value.X + (((Image)sender).Width / 2);
            }

            if (value.Y >= 0 && value.Y <= this.canvasMap.Height && value.Y % 10 == 0)
            {
                Canvas.SetTop((Image)sender, value.Y);
                ((ParkObjBase)_lastSelected.DataContext).Position.Y = value.Y + (((Image)sender).Height / 2);
            }

        }
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _canvas.Build();
    }

    private void Canvas_PointerPressed_1(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.Source != this.canvasMap)
        {
            return;
        }

        Image img = new()
        {
            Source = new Bitmap("Resources/ParkPieces/SM_Pyramid_01.png"),
            Width = 100,
            Height = 100,
        };

        ParkObjBase parkObj = new ParkObjBase("SM_Pyramid_01");

        img.PointerPressed += Rectangle_PointerPressed;
        img.PointerReleased += Rectangle_PointerReleased;
        img.PointerMoved += Rectangle_PointerMoved;
        img.DataContext = parkObj;


        canvasMap.Children.Add(img);
        double x = e.GetCurrentPoint(this.canvasMap).Position.X;
        double y = e.GetCurrentPoint(this.canvasMap).Position.Y;
        Canvas.SetLeft(img, x);
        Canvas.SetTop(img, y);

        parkObj.Position.X = x;
        parkObj.Position.Y = y;


        _canvas.ParkObjs.Add(parkObj);
    }

    private int _angle = 0;

    private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lastSelected == null)
        {
            return;
        }

        _angle += 45;
        _angle %= 360;
        _lastSelected.RenderTransform = new RotateTransform(_angle);
        ((ParkObjBase)_lastSelected.DataContext).Rotation.Z = _angle;
    }
}