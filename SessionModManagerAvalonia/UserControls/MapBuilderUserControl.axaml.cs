using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    bool _dragging = false;
    CanvasViewModel _canvas = new CanvasViewModel();
    Image? _lastSelected;

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

        DataContext = _canvas;
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
            ParkObjBase? dataContext = ((ParkObjBase)_lastSelected.DataContext);

            double x = value.X;
            double y = value.Y;

            var scaledX = Math.Round(x.MapRange(0, canvasMap.Width, 0, _canvas.FloorWidth) + (dataContext.AnchorPointX * dataContext.UnrealScale.X), 0);
            var scaledY = Math.Round(y.MapRange(0, canvasMap.Height, 0, _canvas.FloorHeight) + (dataContext.AnchorPointY * dataContext.UnrealScale.Y), 0);

            if (dataContext.AnchorPointX * dataContext.UnrealScale.X % 5 != 0)
            {
                scaledX = 5 * (int)Math.Round(scaledX / 5.0);
            }
            if (dataContext.AnchorPointY * dataContext.UnrealScale.Y % 5 != 0)
            {
                scaledY = 5 * (int)Math.Round(scaledY / 5.0);
            }


            if (value.X >= 0 && value.X <= this.canvasMap.Width && scaledX % 10 == 0)
            {
                Canvas.SetLeft((Image)sender, x);

                dataContext.Position.X = scaledX;
            }

            if (value.Y >= 0 && value.Y <= this.canvasMap.Height && scaledY % 10 == 0)
            {
                Canvas.SetTop((Image)sender, y);

                dataContext.Position.Y = scaledY;
            }

            Canvas.SetLeft(rectCoords, dataContext.Position.X.MapRange(0, _canvas.FloorWidth, 0, this.canvasMap.Width));
            Canvas.SetTop(rectCoords, dataContext.Position.Y.MapRange(0, _canvas.FloorHeight, 0, this.canvasMap.Height));
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

        ParkObjBase parkObj = null;
        bool isCloning = e.KeyModifiers == KeyModifiers.Control;
        if (isCloning)
        {
            parkObj = (_lastSelected.DataContext as ParkObjBase).Clone();
        }
        else
        {
            parkObj = _canvas.ObjectCatalog[_canvas.ActiveCatalogIndex].Clone();
        }

        Bitmap bitmap = new(parkObj.ImagePath);

        int scaledW = (int)parkObj.UnrealScale.X.MapRange(0, _canvas.FloorWidth, 0, this.canvasMap.Width);
        int scaledH = (int)parkObj.UnrealScale.Y.MapRange(0, _canvas.FloorHeight, 0, this.canvasMap.Height);

        var scaledSize = new PixelSize(Math.Max(5, scaledW), Math.Max(5, scaledH));

        Image img = new()
        {
            Source = bitmap.CreateScaledBitmap(scaledSize),
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

        if (isCloning)
        {
            RotateObject(img, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height);
        }

        _canvas.ParkObjs.Add(parkObj);
        _lastSelected = img;
    }

    private int _angle = 0;

    private void ButtonRotateRight_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lastSelected == null)
        {
            return;
        }

        _angle += 45;
        _angle %= 360;

        RotateObject(_lastSelected, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height);
    }

    private void RotateObject(Image img, double width, double height)
    {
        ParkObjBase? dataContext = (ParkObjBase)img.DataContext;
        if (dataContext == null)
        {
            return;
        }

        double centerX = width / 2;
        double centerY = height / 2;

        if (dataContext.AnchorPointX == 0.5)
        {
            centerX *= 0;
        }
        else if (dataContext.AnchorPointX < 0.5)
        {
            centerX *= -1;
        }

        if (dataContext.AnchorPointY == 0.5)
        {
            centerY *= 0;
        }
        else if (dataContext.AnchorPointY < 0.5)
        {
            centerY *= -1;
        }


        img.RenderTransform = new RotateTransform(_angle, centerX, centerY);
        dataContext.Rotation.Z = _angle;
    }

    private void ButtonRotateLeft_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_lastSelected == null)
        {
            return;
        }

        _angle -= 45;
        
        if (_angle < 0)
        {
            _angle = 360 - 45;
        }

        RotateObject(_lastSelected, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height); 
    }
}