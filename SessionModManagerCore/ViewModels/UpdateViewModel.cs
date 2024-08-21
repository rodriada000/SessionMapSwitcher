using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class UpdateViewModel : ViewModelBase
    {
        /// <summary>
        /// url to the latest github release of the application
        /// </summary>
        public const string LatestReleaseUrl = "https://github.com/rodriada000/SessionMapSwitcher/releases/latest";

        private string _headerMessage;
        private bool _isUpdating;
        private string _newVersionAvailable;
        private double _updatePercent;

        public string HeaderMessage
        {
            get => _headerMessage;
            set
            {
                _headerMessage = value;
                NotifyPropertyChanged();
            }
        }

        public string NewVersionAvailable
        {
            get => _newVersionAvailable;
            set
            {
                _newVersionAvailable = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                _isUpdating = value;
                NotifyPropertyChanged();
            }
        }

        public double UpdatePercent
        {
            get => _updatePercent;
            set
            {
                _updatePercent = value;
                NotifyPropertyChanged();
            }
        }

        public UpdateViewModel()
        {
            HeaderMessage = "A new version of Session Mod Manager is available to download. You can view what's changed below.";
        }
    }
}
