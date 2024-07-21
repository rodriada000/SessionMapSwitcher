using SessionMapSwitcherCore.ViewModels;

namespace SessionModManagerCore.ViewModels
{
    public class AuthorDropdownViewModel : ViewModelBase
    {

        private string _author;
        private int _assetCount;
        private bool _isSelected;

        public string Author
        {
            get
            {
                return _author;
            }
            set
            {
                _author = value;
                NotifyPropertyChanged();
            }
        }

        public string AssetCountDisplayText
        {
            get
            {
                if (AssetCount == 0)
                    return "";

                return $"({AssetCount})";
            }
        }

        public int AssetCount
        {
            get
            {
                return _assetCount;
            }
            set
            {
                _assetCount = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(AssetCountDisplayText));
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public AuthorDropdownViewModel(string author, int assetCount)
        {
            Author = author;
            AssetCount = assetCount;
        }

        public override string ToString()
        {
            return $"Author: {Author} | Count: {AssetCount}";
        }
    }
}
