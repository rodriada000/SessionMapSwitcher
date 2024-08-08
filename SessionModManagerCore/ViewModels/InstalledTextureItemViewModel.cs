using SessionModManagerCore.Classes;
using static SessionModManagerCore.ViewModels.TextureReplacerViewModel;

namespace SessionModManagerCore.ViewModels
{
    public class InstalledTextureItemViewModel : ViewModelBase
    {
        private string _textureName;
        private bool _isSelected;
        private bool _isEnabled;

        public delegate void EnabledChanged(bool isEnabled, TextureMetaData metaData);

        public event EnabledChanged IsEnabledChanged;


        public TextureMetaData MetaData { get; set; }

        public string TextureName
        {
            get { return _textureName; }
            set
            {
                _textureName = value;
                NotifyPropertyChanged();
            }
        }

        public string Category
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.MetaData.Category))
                {
                    return "";
                }

                string trimmedCat = this.MetaData.Category.Replace("session-", "");
                System.Globalization.TextInfo textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;

                return textInfo.ToTitleCase(trimmedCat);
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

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                NotifyPropertyChanged();
                IsEnabledChanged?.Invoke(_isEnabled, MetaData);
            }
        }


        public InstalledTextureItemViewModel(TextureMetaData metaData)
        {
            this.IsSelected = false;
            this.MetaData = metaData;
            this.IsEnabled = metaData.Enabled;
            TextureName = this.MetaData.Name == null ? this.MetaData.AssetName : this.MetaData.Name;
        }

    }
}
