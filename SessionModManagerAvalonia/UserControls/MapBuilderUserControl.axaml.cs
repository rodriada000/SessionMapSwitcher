using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Diagnostics;

namespace SessionModManagerAvalonia;

public partial class MapBuilderUserControl : UserControl
{
    private const int _floorSize = 15000;
    bool _dragging = false;
    UAssetEditor _assetEditor = new UAssetEditor();
    CanvasViewModel _canvas = new CanvasViewModel();
    Image? _lastSelected;
    double _dpi = 96;

    public MapBuilderUserControl()
    {
        InitializeComponent();
        var window = TopLevel.GetTopLevel(this) as Window;

        foreach (var c in _canvas.ObjectCatalog)
        {
            Bitmap bitmap = new(c.ImagePath);

            Image img = new()
            {
                Source = bitmap,//.CreateScaledBitmap(scaledSize),
                DataContext = c,
                Width = 100,
                Height = 100,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both
            };

            img.PointerPressed += OnPointerPressed;

            panelCat.Children.Add(img);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Image? img = ((Image)sender);
        ParkObjBase obj = (ParkObjBase)img.DataContext;
        _canvas.ActiveCatalogIndex = _canvas.ObjectCatalog.IndexOf(obj);
    }

    private void Rectangle_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        _dragging = true;
        _lastSelected = (Image)sender;
        _angle = (int)((ParkObjBase)_lastSelected.DataContext).Rotation.Z;
    }

    private void Rectangle_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        Rectangle_PointerMoved(sender, e);
        _dragging = false;
    }

    private void Rectangle_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_dragging)
        {
            Point value = e.GetPosition(this.canvasMap);


            Image? img = (Image)sender;
            double x = value.X - (img.Bounds.Size.Width / 2);
            double y = value.Y - (img.Bounds.Size.Height / 2);
            x = 5 * (int)Math.Round(x / 5.0);
            y = 5 * (int)Math.Round(y / 5.0);


            ParkObjBase? dataContext = ((ParkObjBase)_lastSelected.DataContext);

            if (value.X >= 0 && value.X <= this.canvasMap.Width && x % 5 == 0)
            {
                Canvas.SetLeft((Image)sender, x);
                dataContext.Position.X = (img.Bounds.X + img.Bounds.Size.Width * dataContext.AnchorPointX).MapRange(0, canvasMap.Width, 0, _floorSize);
            }

            if (value.Y >= 0 && value.Y <= this.canvasMap.Height && y % 5 == 0)
            {
                Canvas.SetTop((Image)sender, y);
                dataContext.Position.Y = (img.Bounds.Y + img.Bounds.Size.Height * dataContext.AnchorPointY).MapRange(0, canvasMap.Height, 0, _floorSize);
            }

            dataContext.OriginalPosition = new ObjVector(img.Bounds.X + img.Bounds.Size.Width * dataContext.AnchorPointX, img.Bounds.Y + img.Bounds.Size.Height * dataContext.AnchorPointY, 0);
            dataContext.CenterPoint = new ObjVector(img.Bounds.X + (img.Bounds.Size.Width / 2.0), img.Bounds.Y + (img.Bounds.Size.Height / 2.0), 0);


            SetPositionBasedOnAngle(dataContext);
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

        ParkObjBase parkObj = _canvas.ObjectCatalog[_canvas.ActiveCatalogIndex].Clone();

        Bitmap bitmap = new(parkObj.ImagePath);

        int scaledW = (int)parkObj.UnrealScale.X.MapRange(0, _floorSize, 0, this.canvasMap.Width * 4);
        int scaledH = (int)parkObj.UnrealScale.Y.MapRange(0, _floorSize, 0, this.canvasMap.Height * 4);
        scaledW = 5 * (int)Math.Round(scaledW / 5.0);
        scaledH = 5 * (int)Math.Round(scaledH / 5.0);

        var scaledSize = new PixelSize(Math.Max(5,scaledW), Math.Max(5,scaledH));

        Image img = new()
        {
            Source = bitmap.CreateScaledBitmap(scaledSize),
            //Width = 100,
            //Height = 100,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both
        };


        img.PointerPressed += Rectangle_PointerPressed;
        img.PointerReleased += Rectangle_PointerReleased;
        img.PointerMoved += Rectangle_PointerMoved;
        img.DataContext = parkObj;


        canvasMap.Children.Add(img);
        double x = e.GetCurrentPoint(this.canvasMap).Position.X;
        double y = e.GetCurrentPoint(this.canvasMap).Position.Y;
        Canvas.SetLeft(img, x);
        Canvas.SetTop(img, y);


        parkObj.Position.X = (x - (scaledSize.Width * parkObj.AnchorPointX)).MapRange(0, canvasMap.Width, 0, _floorSize);
        parkObj.Position.Y = (y - (scaledSize.Height * parkObj.AnchorPointY)).MapRange(0, canvasMap.Height, 0, _floorSize);

        _canvas.ParkObjs.Add(parkObj);
        _lastSelected = img;
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
        ParkObjBase? dataContext = (ParkObjBase)_lastSelected.DataContext;

        SetPositionBasedOnAngle(dataContext);
    }

    private void SetPositionBasedOnAngle(ParkObjBase? dataContext)
    {
        if (dataContext == null || dataContext.CenterPoint == null || dataContext.OriginalPosition == null)
        {
            return;
        }

        dataContext.Rotation.Z = _angle;

        double angleInRadians = Math.PI * _angle / 180;
        var x = dataContext.CenterPoint.X + (dataContext.OriginalPosition.X - dataContext.CenterPoint.X) * Math.Cos(angleInRadians) - (dataContext.OriginalPosition.Y - dataContext.CenterPoint.Y) * Math.Sin(angleInRadians);
        var y = dataContext.CenterPoint.Y + (dataContext.OriginalPosition.X - dataContext.CenterPoint.X) * Math.Sin(angleInRadians) + (dataContext.OriginalPosition.Y - dataContext.CenterPoint.Y) * Math.Cos(angleInRadians);

        Canvas.SetLeft(rectCoords, x);
        Canvas.SetTop(rectCoords, y);

        dataContext.Position.X = x.MapRange(0, this.canvasMap.Width, 0, _floorSize);
        dataContext.Position.Y = y.MapRange(0, this.canvasMap.Height, 0, _floorSize);

    }

    private void Canvas_PointerMoved_1(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        double x = e.GetCurrentPoint(this.canvasMap).Position.X;
        double y = e.GetCurrentPoint(this.canvasMap).Position.Y;

        //labelCoord.Content = $"{x},{y} -> {Math.Round(x.MapRange(0, canvasMap.Width, 0, 15000),2)},{Math.Round(y.MapRange(0, canvasMap.Height, 0, 15000),2)}";
    }
}