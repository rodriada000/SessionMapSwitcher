namespace SessionMapSwitcherCore.ViewModels
{
    public class UpdateViewModel : ViewModelBase
    {

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
