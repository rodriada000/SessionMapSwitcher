using SessionModManagerCore.Classes;

namespace SessionModManagerCore.ViewModels
{
    public class CatalogSubscriptionViewModel : ViewModelBase
    {
        private string _url;
        private string _name;
        private bool _isActive;

        public CatalogSubscriptionViewModel(CatalogSubscription c)
        {
            Url = c.Url;
            Name = c.Name;
            IsActive = c.IsActive;
        }

        public CatalogSubscriptionViewModel(string url, string name)
        {
            Url = url;
            Name = name;
            IsActive = true;
        }

        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                NotifyPropertyChanged();
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                NotifyPropertyChanged();
            }
        }
    }
}
