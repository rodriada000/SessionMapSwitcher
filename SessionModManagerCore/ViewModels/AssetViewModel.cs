using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.ViewModels
{
    public class AssetViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static string dateTimeFormat = "ddd, dd MMM yy";

        private string _name;
        private string _author;
        private string _description;
        private string _assetCategory;
        private string _updatedDate;
        private bool _isSelected;
        private string _version;

        public Asset Asset { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
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

        public string Author
        {
            get { return _author; }
            set
            {
                _author = value;
                NotifyPropertyChanged();
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyPropertyChanged();
            }
        }

        public string AssetCategory
        {
            get { return _assetCategory; }
            set
            {
                _assetCategory = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Category));
            }
        }

        public string Category
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AssetCategory))
                {
                    return "";
                }

                string trimmedCat = AssetCategory.Replace("session-", "");
                System.Globalization.TextInfo textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;

                return textInfo.ToTitleCase(trimmedCat);
            }
        }

        public string UpdatedDate
        {
            get { return _updatedDate; }
            set
            {
                _updatedDate = value;
                NotifyPropertyChanged();
            }
        }

        public string Version
        {
            get { return _version; }
            set
            {
                _version = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Asset is considered out of date if it was before the Session 0.0.0.5 game update (4/24/2020) and
        /// has no mention in the description that it was recooked with Unreal Engine 4.24
        /// </summary>
        public bool IsOutOfDate
        {
            get
            {
                string[] keywords = new string[] { "UE 4.24", "4.24.3", "Updated for Session 0.0.0.5" };
                return this.Asset.UpdatedDate < new DateTime(2020, 4, 24) && keywords.All(k => Description.IndexOf(k, StringComparison.OrdinalIgnoreCase) < 0);
            }
        }

        public AssetViewModel(Asset asset)
        {
            this.Asset = asset;
            Name = asset.Name;
            Description = asset.Description;
            Author = asset.Author;
            AssetCategory = asset.Category;
            UpdatedDate = asset.UpdatedDate == DateTime.MinValue ? "" : asset.UpdatedDate.ToLocalTime().ToString(AssetViewModel.dateTimeFormat);

            Version = asset.Version <= 0 ? "1" : asset.Version.ToString();
            IsSelected = false;
        }
    }
}
