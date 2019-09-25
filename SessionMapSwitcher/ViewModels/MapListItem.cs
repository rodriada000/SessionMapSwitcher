using SessionMapSwitcher.ViewModels;
using System.Collections.Generic;

class MapListItem : ViewModelBase
{
    private string _displayName;
    private string _fullPath;
    private bool _isEnabled = true;

    public string DisplayName
    {
        get { return _displayName; }
        set
        {
            _displayName = value;
            NotifyPropertyChanged();
        }
    }

    public string FullPath { get => _fullPath; set => _fullPath = value; }

    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            _isEnabled = value;
            NotifyPropertyChanged();
        }
    }

    internal string RemoveFileExtensions(string mapFileName)
    {
        List<string> extensionsToRemove = new List<string>() { ".umap", "_BuiltData.uexp", "_BuiltData.uasset", "_BuiltData.ubulk", ".uexp", ".uasset", ".ubulk", };

        string nameWithoutExtensions = mapFileName;

        foreach (string extension in extensionsToRemove)
        {
            nameWithoutExtensions = nameWithoutExtensions.Replace(extension, "");
        }

        return nameWithoutExtensions;
    }
}
