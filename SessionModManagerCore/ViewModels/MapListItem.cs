using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.IO;

public class MapListItem : ViewModelBase
{
    private string _mapName;
    private string _customName;
    private string _fullPath;
    private string _validationHint;
    private string _pathToImage;
    private string _tooltip;
    private bool _isEnabled = true;
    private bool _isSelected = false;
    private bool _isValid = true;
    private bool _isHiddenByUser = false;

    public string CustomName
    {
        get { return _customName; }
        set
        {
            _customName = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(DisplayName);
        }
    }

    public string MapName
    {
        get { return _mapName; }
        set
        {
            _mapName = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(DisplayName);
        }
    }

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(CustomName))
            {
                return MapName;
            }
            return CustomName;
        }
    }

    public string PathToImage
    {
        get { return _pathToImage; }
        set
        {
            _pathToImage = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Absolute path to the .umap file
    /// </summary>
    public string FullPath { get => _fullPath; set => _fullPath = value; }

    /// <summary>
    /// Path to directory where all files related to this map are located.
    /// </summary>
    public string DirectoryPath
    {
        get
        {
            if (String.IsNullOrEmpty(FullPath))
            {
                return "";
            }

            FileInfo mapFileInfo = new FileInfo(FullPath);

            return mapFileInfo.DirectoryName;
        }
    }

    /// <summary>
    /// Returns <see cref="FullPath"/> formatted to work for UserEngine.ini
    /// e.g. "/Game/Path/To/Map"
    /// </summary>
    public string MapPathForIni
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullPath))
            {
                return "";
            }

            return FullPath.Replace(SessionPath.ToContent, "/Game").Replace("\\", "/").Replace(".umap", "");
        }
    }

    public string ValidationHint
    {
        get { return _validationHint; }
        set
        {
            _validationHint = value;
            NotifyPropertyChanged();
        }
    }

    public string Tooltip
    {
        get
        {
            if (_tooltip == null && !string.IsNullOrWhiteSpace(DirectoryPath))
            {
                _tooltip = DirectoryPath;
            }

            return _tooltip;
        }
        set
        {
            _tooltip = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsHiddenByUser
    {
        get { return _isHiddenByUser; }
        set
        {
            _isHiddenByUser = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            _isEnabled = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get { return _isSelected; }
        set
        {
            _isSelected = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsValid
    {
        get { return _isValid; }
        set
        {
            _isValid = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsDefaultMap { get; set; }

    public string GameDefaultMapSetting { get; set; }
    public string GlobalDefaultGameModeSetting { get; set; }

    public MapListItem()
    {
        FullPath = "";
        MapName = "";
    }

    public MapListItem(string pathToMapFile)
    {
        FullPath = pathToMapFile;
        MapName = Path.GetFileNameWithoutExtension(pathToMapFile);

        Validate();
    }

    public void Validate()
    {
        IsValid = true;

        if (File.Exists(FullPath) == false)
        {
            IsValid = false;
            ValidationHint = "(file missing)";
            return;
        }

        if (MapListItem.HasGameMode(FullPath) == false)
        {
            IsValid = false;
            ValidationHint = "(missing gamemode)";
        }
    }

    /// <summary>
    /// Reads the file and looks for the string '/Game/Data/PBP_InGameSessionGameMode'
    /// </summary>
    /// <param name="fullPath"> full path to file </param>
    public static bool HasGameMode(string fullPath)
    {
        try
        {
            if (!File.Exists(fullPath))
            {
                return false;
            }

            string umapContents = File.ReadAllText(fullPath);
            return umapContents.IndexOf("/Game/Data/PBP_InGameSessionGameMode", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override bool Equals(object obj)
    {
        if (!(obj is MapListItem))
        {
            return false;
        }

        return (obj as MapListItem).FullPath == this.FullPath && (obj as MapListItem).MapName == this.MapName;
    }
}
