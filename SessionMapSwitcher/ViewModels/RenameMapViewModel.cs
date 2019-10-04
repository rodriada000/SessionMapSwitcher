using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SessionMapSwitcher.ViewModels
{
    public class RenameMapViewModel : ViewModelBase
    {
        private string _customMapName;
        private MapListItem _mapToRename;

        public string CustomMapName
        {
            get { return _customMapName; }
            set
            {
                _customMapName = value;
                NotifyPropertyChanged();
            }
        }

        public string RenameWindowTitle
        {
            get
            {
                return $"Rename Map ({MapToRename.MapName})";
            }
        }

        public MapListItem MapToRename
        {
            get => _mapToRename;
            set
            {
                _mapToRename = value;
                NotifyPropertyChanged(nameof(RenameWindowTitle));
            }
        }

        public RenameMapViewModel(MapListItem map)
        {
            MapToRename = map;
            CustomMapName = map.CustomName;
        }

        internal bool ValidateAndSetCustomName()
        {
            bool isValid = true;
            string errorMsg = "";

            if (MapToRename == null)
            {
                isValid = false;
                errorMsg = "MapToRename is null.";
            }
            else if (String.IsNullOrEmpty(CustomMapName))
            {
                isValid = false;
                errorMsg = "No custom name entered.";
            }
            else if (CustomMapName == MapToRename.MapName)
            {
                isValid = false;
                errorMsg = "The custom name must be different than the actual Map Name.";
            }
            else if (CustomMapName == MapToRename.CustomName)
            {
                isValid = false;
                errorMsg = "The custom name was not changed.";
            }

            if (isValid)
            {
                MapToRename.CustomName = CustomMapName;
            }
            else
            {
                MessageBox.Show($"Invalid name: {errorMsg}", "Invalid Name!", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return isValid;
        }
    }
}
