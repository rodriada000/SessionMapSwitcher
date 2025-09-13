using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SessionModManagerAvalonia;

public partial class MapBuilderUserControl : UserControl
{
    bool _dragging = false;
    CanvasViewModel _canvas = new CanvasViewModel();
    Image? _lastSelected;
    Rectangle rectCoords = new Rectangle()
    {
        Fill = new SolidColorBrush(Colors.Red),
        Width = 2,
        Height = 2,
        ZIndex = 100,
    };

    private readonly ZoomBorder? _zoomBorder;

    public MapBuilderUserControl()
    {
        InitializeComponent();
        var window = TopLevel.GetTopLevel(this) as Window;

        foreach (var c in _canvas.ObjectCatalog)
        {
            var img = new ParkObjectUserControl(c);
            img.PointerPressed += OnPointerPressed_SelectCatalogObject;
            panelCat.Children.Add(img);
        }

        DataContext = _canvas;
        canvasMap.Children.Add(rectCoords);
    }

    private void OnPointerPressed_SelectCatalogObject(object? sender, PointerPressedEventArgs e)
    {
        ParkObjectUserControl? img = ((ParkObjectUserControl)sender);
        _canvas.ActiveCatalogIndex = _canvas.ObjectCatalog.IndexOf(img.ViewModel);
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

            var scaledX = Math.Round(x.MapRange(canvasMap.Width, _canvas.FloorWidth) + (dataContext.AnchorPointX * dataContext.UnrealScale.X), 0);
            var scaledY = Math.Round(y.MapRange(canvasMap.Height, _canvas.FloorHeight) + (dataContext.AnchorPointY * dataContext.UnrealScale.Y), 0);

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

            Canvas.SetLeft(rectCoords, dataContext.Position.X.MapRange(_canvas.FloorWidth, this.canvasMap.Width));
            Canvas.SetTop(rectCoords, dataContext.Position.Y.MapRange(_canvas.FloorHeight, this.canvasMap.Height));
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

        double x = e.GetCurrentPoint(this.canvasMap).Position.X;
        double y = e.GetCurrentPoint(this.canvasMap).Position.Y;

        Image img = AddObjToCanvas(parkObj, x, y);

        if (isCloning)
        {
            ((ParkObjBase)img.DataContext).Rotation.Z = ((ParkObjBase)_lastSelected.DataContext).Rotation.Z;
            RotateObject(img, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height);
        }

        _canvas.ParkObjs.Add(parkObj);
        _lastSelected = img;
    }

    private Image AddObjToCanvas(ParkObjBase parkObj, double x, double y)
    {
        Bitmap bitmap = new(parkObj.ImagePath);

        int scaledW = (int)parkObj.UnrealScale.X.MapRange(_canvas.FloorWidth, this.canvasMap.Width);
        int scaledH = (int)parkObj.UnrealScale.Y.MapRange(_canvas.FloorHeight, this.canvasMap.Height);

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

        Canvas.SetLeft(img, x);
        Canvas.SetTop(img, y);
        return img;
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

        ((ParkObjBase)_lastSelected.DataContext).Rotation.Z = _angle;
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


        img.RenderTransform = new RotateTransform(dataContext.Rotation.Z, centerX, centerY);
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

        ((ParkObjBase)_lastSelected.DataContext).Rotation.Z = _angle;
        RotateObject(_lastSelected, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height);
    }


    private async void ButtonLoad_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        IReadOnlyList<FilePickerFileType> filters = new List<FilePickerFileType>() { new FilePickerFileType("SMM Park File (*.json)") { Patterns = new List<string>() { "*.json" } } };
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Park File",
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        if (files.Any())
        {
            var loaded = _canvas.LoadPark(files[0].Path.AbsolutePath);

            if (loaded.Count > 0)
            {
                canvasMap.Children.Clear();
                canvasMap.Children.Add(rectCoords);

                foreach (var obj in loaded)
                {
                    Image img = AddObjToCanvas(obj, 0, 0);
                    img.RenderTransform = new RotateTransform(obj.Rotation.Z, obj.CenterX, obj.CenterY);
                    Canvas.SetLeft(img, obj.Left);
                    Canvas.SetTop(img, obj.Top);
                }
            }
        }
    }
    private async void ButtonSave_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        IReadOnlyList<FilePickerFileType> filters = new List<FilePickerFileType>() { new FilePickerFileType("SMM Park File (*.json)") { Patterns = new List<string>() { "*.json" } } };
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Park File (.json)",
            FileTypeChoices = filters,
        });

        if (file is not null)
        {
            List<ParkItemData> data = new List<ParkItemData>();
            foreach (var child in canvasMap.Children)
            {
                if (child is Image)
                {
                    var img = (Image)child;
                    data.Add(new ParkItemData((ParkObjBase)child.DataContext)
                    {
                        CenterX = (img.RenderTransform as RotateTransform)?.CenterX ?? 0,
                        CenterY = (img.RenderTransform as RotateTransform)?.CenterY ?? 0,
                        Top = img.Bounds.Top,
                        Left = img.Bounds.Left,
                    });
                }
            }

            _canvas.SavePark(file.Path.AbsolutePath, data);
        }
    }

    private void UserControl_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (_lastSelected == null)
        {
            return;
        }

        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            var i = canvasMap.Children.IndexOf(_lastSelected);
            if (i >= 0)
            {
                canvasMap.Children.RemoveAt(i);
                _canvas.ParkObjs.Remove((ParkObjBase)_lastSelected.DataContext);
                _lastSelected.PointerPressed -= Rectangle_PointerPressed;
                _lastSelected.PointerReleased -= Rectangle_PointerReleased;
                _lastSelected.PointerMoved -= Rectangle_PointerMoved;
                _lastSelected = null;
            }
        }
    }

    private void Grid_KeyUp_1(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Right)
        {
            ButtonRotateRight_Click(sender, e);
        }
        else if (e.Key == Key.Left)
        {
            ButtonRotateLeft_Click(sender, e);
        }
    }
}