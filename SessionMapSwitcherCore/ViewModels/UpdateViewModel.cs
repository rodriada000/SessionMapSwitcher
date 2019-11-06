namespace SessionMapSwitcherCore.ViewModels
{
    public class UpdateViewModel : ViewModelBase
    {
        /// <summary>
        /// url to the latest github release of the application
        /// </summary>
        public const string LatestReleaseUrl = "https://github.com/rodriada000/SessionMapSwitcher/releases/latest";

        private string _headerMessage;
        private bool _isBrowserVisible;

        public string HeaderMessage
        {
            get => _headerMessage;
            set
            {
                _headerMessage = value;
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
            HeaderMessage = "A new version of Session Map Switcher is available to download. You can view what's changed below.";
            IsBrowserVisible = false;
        }
    }
}
