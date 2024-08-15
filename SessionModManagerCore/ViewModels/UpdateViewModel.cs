using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using System.IO;

namespace SessionModManagerCore.ViewModels
{
    public class UpdateViewModel : ViewModelBase
    {
        public class NewVersionJson
        {
            public string TargetVersion { get; set; }
        }

        /// <summary>
        /// url to the latest github release of the application
        /// </summary>
        public const string LatestReleaseUrl = "https://github.com/rodriada000/SessionMapSwitcher/releases/latest";

        private string _headerMessage;
        private bool _isBrowserVisible;
        private string _newVersionAvailable;

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

        public bool IsBrowserVisible
        {
            get => _isBrowserVisible;
            set
            {
                _isBrowserVisible = value;
                NotifyPropertyChanged();
            }
        }

        public UpdateViewModel()
        {
            HeaderMessage = "A new version of Session Mod Manager is available to download. You can view what's changed below.";
            IsBrowserVisible = false;
        }

        public string ReadNewVersionFromAgFilesJson()
        {
            string filePath = Path.Combine(SessionPath.ToApplicationRoot, "agbin", "ag_files.json");
            if (File.Exists(filePath))
            {
                var newVersion = JsonConvert.DeserializeObject<NewVersionJson>(File.ReadAllText(filePath));
                NewVersionAvailable = newVersion?.TargetVersion;
                HeaderMessage = $"Version {NewVersionAvailable} of Session Mod Manager is now available to download.\nRelease Notes:";
            }

            return NewVersionAvailable;
        }
    }
}
