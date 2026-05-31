using SessionModManagerCore.Classes;

namespace SessionModManagerCore.ViewModels
{
    public class ParkCatalogObjViewModel : ViewModelBase
    {
        private bool _isSelected;
        private string _name;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        public ParkObjBase ObjectData { get; set; }

        public ParkCatalogObjViewModel()
        {
        }
    }
}
