using System;
using System.IO;

class MapListItem : ViewModelBase
{
    private string _displayName;
    private string _fullPath;
    private string _validationHint;
    private string _tooltip;
    private bool _isEnabled = true;
    private bool _isSelected = false;
    private bool _isValid = true;

    public string DisplayName
    {
        get { return _displayName; }
        set
        {
            _displayName = value;
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

            int lastIndex = FullPath.LastIndexOf("\\");
            if (lastIndex < 0)
            {
                return "";
            }

            return FullPath.Substring(0, lastIndex);
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
        get { return _tooltip; }
        set
        {
            _tooltip = value;
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

    internal void Validate()
    {
        IsValid = true;

        if (File.Exists(FullPath) == false)
        {
            IsValid = false;
            ValidationHint = "(file missing)";
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
    internal static bool HasGameMode(string fullPath)
    {
        try
        {
            string umapContents = File.ReadAllText(fullPath);

            return umapContents.Contains("/Game/Data/PBP_InGameSessionGameMode");
        }
        catch (Exception e)
        {
            return false;
        }
    }
}
