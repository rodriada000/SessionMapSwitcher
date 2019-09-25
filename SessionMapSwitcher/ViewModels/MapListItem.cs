using SessionMapSwitcher.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

class MapListItem : ViewModelBase
{
    private string _displayName;
    private string _fullPath;
    private string _validationHint;
    private bool _isEnabled = true;
    private bool _isSelected = true;
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

    public string FullPath { get => _fullPath; set => _fullPath = value; }

    public string ValidationHint
    {
        get { return _validationHint; }
        set
        {
            _validationHint = value;
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

        string umapContents = File.ReadAllText(FullPath);

        if (umapContents.Contains("/Game/Data/PBP_InGameSessionGameMode") == false)
        {
            IsValid = false;
            ValidationHint = "(missing gamemode)";
        }
    }
}
