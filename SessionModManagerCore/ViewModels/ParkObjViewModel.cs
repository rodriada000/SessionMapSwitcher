using SessionModManagerCore.Classes;

namespace SessionModManagerCore.ViewModels
{
    public class ParkObjViewModel : ViewModelBase
    {
        private bool _isSelected;
        private bool _isPlayerStart;
        private int _currentFloorLevel;

        public int CurrentFloorLevel
        {

            get { return _currentFloorLevel; }
            set
            {
                _currentFloorLevel = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(OnSameFloor));
            }
        }

        public bool OnSameFloor
        {
            get { return _currentFloorLevel == ObjectData.Layer; }
        }


        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsPlayerStart
        {
            get => _isPlayerStart;
            set
            {
                _isPlayerStart = value;
                NotifyPropertyChanged();
            }
        }



        public ParkObjBase ObjectData { get; set; }

        public ParkObjViewModel()
        {
        }
    }
}
