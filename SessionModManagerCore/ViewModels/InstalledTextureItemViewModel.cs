using SessionModManagerCore.Classes;

namespace SessionModManagerCore.ViewModels
{
    public class InstalledTextureItemViewModel : ViewModelBase
    {
        private string _textureName;
        private bool _isSelected;

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

        public InstalledTextureItemViewModel(TextureMetaData metaData)
        {
            this.IsSelected = false;
            this.MetaData = metaData;
            TextureName = this.MetaData.Name == null ? this.MetaData.AssetName : this.MetaData.Name;
        }

    }
}
