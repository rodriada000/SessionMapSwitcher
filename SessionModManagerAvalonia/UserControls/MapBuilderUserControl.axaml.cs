using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SessionMapSwitcherCore.Classes;
using SessionModManagerAvalonia.Classes;
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
    ParkObjImageUserControl? _lastSelected;
    readonly ParkObjBase _playerStart = new ParkObjBase()
    {
        Name = "PLAYER_START",
        Layer = 0,
        Position = new ObjVector(200, 200, 200),
        Rotation = new ObjVector(0, 0, 0),
        Scale = new ObjVector(1, 1, 1),
        AnchorPoint = new ObjVector(0.5, 0.5, 0),
        IsPlayerStart = true,
        UnrealScale = new ObjVector(100, 100, 100),
    };
    Rectangle rectCoords = new Rectangle()
    {
        Fill = new SolidColorBrush(Colors.Red),
        Width = 2,
        Height = 2,
        ZIndex = 100,
    };
    private readonly GridBackground _gridLines = new GridBackground()
    {
        GridSpacing = 40,
        GridThickness = 1,
        GridBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
        IsHitTestVisible = false,
    };

    private readonly ZoomBorder? _zoomBorder;

    public MapBuilderUserControl()
    {
        InitializeComponent();
        var window = TopLevel.GetTopLevel(this) as Window;

        AddCatalogItems();

        DrawGridLines();

        DataContext = _canvas;
        //canvasMap.Children.Add(rectCoords);
        AddObjToCanvas(_playerStart, 0, 0);
        _canvas.ParkObjs.Add(_playerStart);
    }

    private void DrawGridLines()
    {
        canvasMap.Children.Remove(_gridLines);

        _gridLines.Width = canvasMap.Bounds.Width;
        _gridLines.Height = canvasMap.Bounds.Height;
        _gridLines.GridSpacing = _canvas.GridSnapValue;
        _gridLines.ZIndex = 0;

        canvasMap.Children.Add(_gridLines);
    }

    private void AddCatalogItems()
    {
        for (int i = 0; i < _canvas.ObjectCatalog.Count; i++)
        {
            ParkObjBase? c = _canvas.ObjectCatalog[i];
            if (i == 0)
            {
                c.IsSelected = true;
            }
            var img = new ParkObjectUserControl(c);
            img.PointerPressed += OnPointerPressed_SelectCatalogObject;
            panelCat.Children.Add(img);
        }
    }

    private void OnPointerPressed_SelectCatalogObject(object? sender, PointerPressedEventArgs e)
    {
        foreach (var item in panelCat.Children)
        {
            if (item is ParkObjectUserControl poc)
            {
                poc.ViewModel.IsSelected = false;
            }
        }

        ParkObjectUserControl? img = ((ParkObjectUserControl?)sender);
        if (img != null)
        {
            img.ViewModel.IsSelected = true;
        }

        _canvas.ActiveCatalogIndex = _canvas.ObjectCatalog.IndexOf(img?.ViewModel?.ObjectData);
    }

    /// <summary>
    /// Start dragging a object on the canvs (if on the same layer)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Rectangle_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_lastSelected != null)
        {
            _lastSelected.ViewModel.IsSelected = false;
        }

        _lastSelected = (ParkObjImageUserControl?)sender;

        if (_lastSelected?.ObjectData.Layer == _canvas.CurrentFloorLayer)
        {
            _lastSelected.ViewModel.IsSelected = true;
            _dragging = true;
            _angle = (int)_lastSelected.ObjectData.Rotation.Z;
        }
        else
        {
            _lastSelected = null;
            _dragging = false;
        }
    }

    private void Rectangle_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (_dragging)
        {
            Rectangle_PointerMoved(sender, e);
        }

        _dragging = false;
    }

    /// <summary>
    /// Move object on canvas grid if user is dragging an object
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Rectangle_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_dragging || sender == null || _lastSelected == null)
        {
            return;
        }

        Point value = e.GetPosition(this.canvasMap);


        double x = value.X;
        double y = value.Y;

        if (x % _canvas.GridSnapValue != 0)
        {
            x = _canvas.GridSnapValue * (int)Math.Round(x / _canvas.GridSnapValue);
        }
        if (y % _canvas.GridSnapValue != 0)
        {
            y = _canvas.GridSnapValue * (int)Math.Round(y / _canvas.GridSnapValue);
        }

        ParkObjImageUserControl img = (ParkObjImageUserControl)sender;
        ParkObjBase objData = _lastSelected.ObjectData;

        var scaledX = Math.Round(x.MapRange(canvasMap.Width, _canvas.FloorWidth) + (objData.AnchorPoint.X * objData.UnrealScale.X), 0);
        var scaledY = Math.Round(y.MapRange(canvasMap.Height, _canvas.FloorHeight) + (objData.AnchorPoint.Y * objData.UnrealScale.Y), 0);
        var zPos = (objData.UnrealScale.Z * objData.Layer) + (objData.AnchorPoint.Z * objData.UnrealScale.Z);

        if (objData.AnchorPoint.X * objData.UnrealScale.X % 5 != 0)
        {
            scaledX = 5 * (int)Math.Round(scaledX / 5.0);
        }
        if (objData.AnchorPoint.Y * objData.UnrealScale.Y % 5 != 0)
        {
            scaledY = 5 * (int)Math.Round(scaledY / 5.0);
        }
        if (objData.AnchorPoint.Z * objData.UnrealScale.Z % 5 != 0)
        {
            zPos = 5 * (int)Math.Round(zPos / 5.0);
        }


        if (value.X >= 0 && value.X <= this.canvasMap.Width)
        {
            Canvas.SetLeft(img, x);

            objData.Position.X = scaledX;
        }

        if (value.Y >= 0 && value.Y <= this.canvasMap.Height)
        {
            Canvas.SetTop(img, y);

            objData.Position.Y = scaledY;
        }

        objData.Position.Z = zPos;

        //Canvas.SetLeft(rectCoords, objData.Position.X.MapRange(_canvas.FloorWidth, this.canvasMap.Width));
        //Canvas.SetTop(rectCoords, objData.Position.Y.MapRange(_canvas.FloorHeight, this.canvasMap.Height));
    }

    /// <summary>
    /// Build the park 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _canvas.Build();
    }

    /// <summary>
    /// Create (or clone) a new park object and place on canvas grid
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Canvas_PointerPressed_SpawnObject(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse || !e.GetCurrentPoint(this.canvasMap).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source != this.canvasMap)
        {
            return;
        }

        ParkObjBase parkObj;
        bool isCloning = e.KeyModifiers == KeyModifiers.Control;
        if (isCloning && _lastSelected != null && !_lastSelected.ObjectData.IsPlayerStart)
        {
            parkObj = _lastSelected.ObjectData.Clone();
        }
        else
        {
            parkObj = _canvas.ObjectCatalog[_canvas.ActiveCatalogIndex].Clone();
        }

        parkObj.Layer = _canvas.CurrentFloorLayer;

        double x = e.GetCurrentPoint(this.canvasMap).Position.X;
        double y = e.GetCurrentPoint(this.canvasMap).Position.Y;

        parkObj.Position.X = Math.Round(x.MapRange(canvasMap.Width, _canvas.FloorWidth) + (parkObj.AnchorPoint.X * parkObj.UnrealScale.X), 0);
        parkObj.Position.Y = Math.Round(y.MapRange(canvasMap.Height, _canvas.FloorHeight) + (parkObj.AnchorPoint.Y * parkObj.UnrealScale.Y), 0);
        parkObj.Position.Z = (parkObj.UnrealScale.Z * parkObj.Layer) + (parkObj.AnchorPoint.Z * parkObj.UnrealScale.Z * parkObj.Layer);

        var img = AddObjToCanvas(parkObj, x, y);
        img.ViewModel.CurrentFloorLevel = _canvas.CurrentFloorLayer;
        img.ZIndex = _canvas.CurrentFloorLayer + 1;

        if (isCloning && _lastSelected != null)
        {
            img.ObjectData.Rotation.Z = _lastSelected.ObjectData.Rotation.Z;
            RotateObject(img, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height);
        }

        _canvas.ParkObjs.Add(parkObj);

        if (_lastSelected != null)
        {
            _lastSelected.ViewModel.IsSelected = false;
        }

        _lastSelected = img;
        _lastSelected.ViewModel.IsSelected = true;
    }

    private ParkObjImageUserControl AddObjToCanvas(ParkObjBase parkObj, double x, double y)
    {
        int scaledW = (int)parkObj.UnrealScale.X.MapRange(_canvas.FloorWidth, this.canvasMap.Width);
        int scaledH = (int)parkObj.UnrealScale.Y.MapRange(_canvas.FloorHeight, this.canvasMap.Height);

        var scaledSize = new PixelSize(Math.Max(5, scaledW), Math.Max(5, scaledH));

        ParkObjImageUserControl imgControl = new ParkObjImageUserControl(parkObj, scaledSize);
        imgControl.ZIndex = parkObj.Layer + 1;

        imgControl.PointerPressed += Rectangle_PointerPressed;
        imgControl.PointerReleased += Rectangle_PointerReleased;
        imgControl.PointerMoved += Rectangle_PointerMoved;

        canvasMap.Children.Add(imgControl);

        Canvas.SetLeft(imgControl, x);
        Canvas.SetTop(imgControl, y);
        return imgControl;
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

        _lastSelected.ObjectData.Rotation.Z = _angle;
        RotateObject(_lastSelected, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height);
    }

    private void RotateObject(ParkObjImageUserControl img, double width, double height)
    {
        ParkObjBase? objData = img.ObjectData;
        if (objData == null)
        {
            return;
        }

        double centerX = width / 2;
        double centerY = height / 2;

        if (objData.AnchorPoint.X == 0.5)
        {
            centerX *= 0;
        }
        else if (objData.AnchorPoint.X < 0.5)
        {
            centerX *= -1;
        }

        if (objData.AnchorPoint.Y == 0.5)
        {
            centerY *= 0;
        }
        else if (objData.AnchorPoint.Y < 0.5)
        {
            centerY *= -1;
        }


        img.RenderTransform = new RotateTransform(objData.Rotation.Z, centerX, centerY);
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

        _lastSelected.ObjectData.Rotation.Z = _angle;
        RotateObject(_lastSelected, _lastSelected.Bounds.Width, _lastSelected.Bounds.Height);
    }


    /// <summary>
    /// Load park file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
                //canvasMap.Children.Add(rectCoords);

                foreach (var obj in loaded)
                {
                    ParkObjImageUserControl img = AddObjToCanvas(obj, 0, 0);
                    img.RenderTransform = new RotateTransform(obj.Rotation.Z, obj.CenterX, obj.CenterY);
                    img.ZIndex = obj.Layer + 1;
                    Canvas.SetLeft(img, obj.Left);
                    Canvas.SetTop(img, obj.Top);
                }
            }
        }
    }

    /// <summary>
    /// Save park to file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
                if (child is ParkObjImageUserControl)
                {
                    var img = (ParkObjImageUserControl)child;
                    data.Add(new ParkItemData(img.ObjectData)
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
            DeleteSelected();
        }
    }

    private void DeleteSelected()
    {
        if (_lastSelected == null || _lastSelected.ObjectData.IsPlayerStart)
        {
            return;
        }

        var i = canvasMap.Children.IndexOf(_lastSelected);
        if (i >= 0)
        {
            canvasMap.Children.RemoveAt(i);
            _canvas.ParkObjs.Remove(_lastSelected.ObjectData);
            _lastSelected.PointerPressed -= Rectangle_PointerPressed;
            _lastSelected.PointerReleased -= Rectangle_PointerReleased;
            _lastSelected.PointerMoved -= Rectangle_PointerMoved;
            _lastSelected = null;
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
        else if (e.Key == Key.Up)
        {
            Button_Click_GoUpLayer(sender, e);
        }
        else if (e.Key == Key.Down)
        {
            Button_Click_GoDownLayer(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            if (_lastSelected != null)
            {
                _lastSelected.ViewModel.IsSelected = false;
                _lastSelected = null;
            }
        }
    }

    private void Button_Click_StartSession(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var mapSelection = new MapSelectionViewModel();
        mapSelection.LoadAvailableMaps();
        mapSelection.LoadMap("ModularPark");

        MapSelectionViewModel.StartSessionExe();
    }

    private void Button_Click_DeleteSelected(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DeleteSelected();
    }

    private void Button_Click_GoUpLayer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this._canvas.CurrentFloorLayer++;
        UpdateFloorLayerOnChildren();
    }

    private void UpdateFloorLayerOnChildren()
    {
        foreach (var child in canvasMap.Children)
        {
            if (child is ParkObjImageUserControl)
            {
                var img = (ParkObjImageUserControl)child;

                if (_lastSelected == img)
                {
                    img.ViewModel.ObjectData.Layer = this._canvas.CurrentFloorLayer;
                    img.ZIndex = this._canvas.CurrentFloorLayer + 1;
                }

                img.ViewModel.CurrentFloorLevel = this._canvas.CurrentFloorLayer;

            }
        }
    }

    private void Button_Click_GoDownLayer(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_canvas.CurrentFloorLayer > 0)
        {
            this._canvas.CurrentFloorLayer--;
            UpdateFloorLayerOnChildren();
        }
    }

    private void Button_Click_DeleteAllObjects(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        foreach (Control img in canvasMap.Children)
        {
            img.PointerPressed -= Rectangle_PointerPressed;
            img.PointerReleased -= Rectangle_PointerReleased;
            img.PointerMoved -= Rectangle_PointerMoved;
        }

        canvasMap.Children.Clear();
        DrawGridLines();
        //canvasMap.Children.Add(rectCoords);

        _canvas.ParkObjs.Clear();
        AddObjToCanvas(_playerStart, 0, 0);
        _canvas.ParkObjs.Add(_playerStart);

        _lastSelected = null;
    }

    private void txtFloorSize_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var isValidWidth = CanvasViewModel.ValidateFloorSize(_canvas.FloorWidthText);
        var isValidHeight = CanvasViewModel.ValidateFloorSize(_canvas.FloorHeightText);

        if (isValidWidth.Result)
        {
            _canvas.FloorWidth = int.Parse(_canvas.FloorWidthText);
        }
        else
        {
            _canvas.FloorWidthText = _canvas.FloorWidth.ToString();
        }

        if (isValidHeight.Result)
        {
            _canvas.FloorHeight = int.Parse(_canvas.FloorHeightText);
        }
        else
        {
            _canvas.FloorHeightText = _canvas.FloorHeight.ToString();
        }

        if (!isValidWidth.Result)
        {
            MessageService.Instance.ShowMessage(isValidWidth.Message);
        }
        else if (!isValidHeight.Result)
        {
            MessageService.Instance.ShowMessage(isValidHeight.Message);
        }
        else
        {
            MessageService.Instance.ShowMessage("Park size updated.");
        }
    }

    private void canvasMap_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        DrawGridLines();
    }

    private void NumericUpDown_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        DrawGridLines();
    }
}