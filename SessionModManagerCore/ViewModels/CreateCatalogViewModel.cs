using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.ViewModels
{
    public class CreateCatalogViewModel : ViewModelBase
    {
        private string _catalogName;
        private string _selectedAssetName;
        private string _selectedAssetAuthor;
        private string _selectedAssetDescription;
        private string _selectedAssetCategory;
        private string _selectedAssetUpdatedDate;
        private string _selectedAssetVersion;
        private string _selectedAssetID;
        private string _selectedAssetImageUrl;
        private string _selectedAssetDownloadType;
        private string _selectedAssetDownloadUrl;
        private List<string> _downloadTypeList;
        private List<string> _categoryList;
        private ObservableCollection<AssetViewModel> _assetList;
        private AssetViewModel _selectedAsset;
        private List<string> _idExtensions;
        private string _selectedIDExtension;

        public delegate void OnInvalidUpdate(string validationMessage);
        public event OnInvalidUpdate UpdatedAssetInvalid;

        public string CatalogName
        {
            get { return _catalogName; }
            set
            {
                _catalogName = value;
                NotifyPropertyChanged();
            }
        }

        internal Asset AssetToEdit { get; set; }

        public AssetViewModel SelectedAsset
        {
            get { return _selectedAsset; }
            set
            {
                if (_selectedAsset != value)
                {
                    if (AssetToEdit != null)
                    {
                        BoolWithMessage didUpdate = UpdateAsset(_selectedAsset);
                        if (!didUpdate.Result)
                        {
                            return;
                        }
                    }

                    _selectedAsset = value;

                    if (_selectedAsset != null)
                    {
                        AssetToEdit = _selectedAsset.Asset;
                        SelectedAssetAuthor = AssetToEdit.Author;
                        SelectedAssetCategory = AssetToEdit.Category;
                        SelectedAssetDescription = AssetToEdit.Description;
                        SelectedAssetID = AssetToEdit.IDWithoutExtension;
                        SelectedIDExtension = AssetToEdit.ID.Substring(AssetToEdit.ID.Length - 4, 4);
                        SelectedAssetImageUrl = AssetToEdit.PreviewImage;
                        SelectedAssetName = AssetToEdit.Name;
                        SelectedAssetUpdatedDate = AssetToEdit.UpdatedDate.ToLocalTime().ToString("MM/dd/yyyy");
                        SelectedAssetVersion = AssetToEdit.Version.ToString();

                        AssetCatalog.TryParseDownloadUrl(AssetToEdit.DownloadLink, out DownloadLocationType downloadType, out string url);
                        SelectedAssetDownloadUrl = url;

                        SelectedAssetDownloadType = "Url";
                        if (downloadType == DownloadLocationType.GDrive)
                        {
                            SelectedAssetDownloadType = "Google Drive";
                        }
                        if (downloadType == DownloadLocationType.MegaFile)
                        {
                            SelectedAssetDownloadType = "Mega";
                        }
                    }
                    else
                    {
                        ClearSelectedAsset();
                    }
                }

                NotifyPropertyChanged();

            }
        }

        public string SelectedAssetName
        {
            get { return _selectedAssetName; }
            set
            {
                _selectedAssetName = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetAuthor
        {
            get { return _selectedAssetAuthor; }
            set
            {
                _selectedAssetAuthor = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetDescription
        {
            get { return _selectedAssetDescription; }
            set
            {
                _selectedAssetDescription = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetCategory
        {
            get { return _selectedAssetCategory; }
            set
            {
                _selectedAssetCategory = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetUpdatedDate
        {
            get { return _selectedAssetUpdatedDate; }
            set
            {
                _selectedAssetUpdatedDate = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetVersion
        {
            get { return _selectedAssetVersion; }
            set
            {
                _selectedAssetVersion = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetID
        {
            get
            {
                if (_selectedAssetID.EndsWith(".rar", StringComparison.InvariantCultureIgnoreCase) || _selectedAssetID.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                {
                    _selectedAssetID = _selectedAssetID.Substring(0, _selectedAssetID.Length - 4);
                }

                return _selectedAssetID;
            }
            set
            {
                _selectedAssetID = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetImageUrl
        {
            get { return _selectedAssetImageUrl; }
            set
            {
                _selectedAssetImageUrl = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetDownloadType
        {
            get { return _selectedAssetDownloadType; }
            set
            {
                _selectedAssetDownloadType = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DownloadText));
                NotifyPropertyChanged(nameof(DownloadTooltip));

            }
        }

        public string SelectedAssetDownloadUrl
        {
            get { return _selectedAssetDownloadUrl; }
            set
            {
                _selectedAssetDownloadUrl = value;
                NotifyPropertyChanged();
            }
        }

        public string DownloadText
        {
            get
            {
                if (SelectedAssetDownloadType == "Url")
                {
                    return "Url:";
                }
                else if (SelectedAssetDownloadType == "Google Drive")
                {
                    return "Drive ID:";
                }
                else if (SelectedAssetDownloadType == "Mega")
                {
                    return "File ID:";
                }

                return "Url:";
            }
        }

        public string DownloadTooltip
        {
            get
            {
                if (SelectedAssetDownloadType == "Url")
                {
                    return "Enter url to the direct download";
                }
                else if (SelectedAssetDownloadType == "Mega")
                {
                    return "Mega File ID (found in download url)";
                }

                return "Google Drive ID of file (found in the google drive url)";
            }
        }

        public List<string> DownloadTypeList
        {
            get { return _downloadTypeList; }
            set
            {
                _downloadTypeList = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> CategoryList
        {
            get { return _categoryList; }
            set
            {
                _categoryList = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<AssetViewModel> AssetList
        {
            get { return _assetList; }
            set
            {
                _assetList = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> IDExtensions
        {
            get
            {
                if (_idExtensions == null)
                {
                    _idExtensions = new List<string>()
                    {
                        ".zip",
                        ".rar"
                    };
                }

                return _idExtensions;
            }
        }

        public string SelectedIDExtension
        {
            get { return _selectedIDExtension; }
            set
            {
                _selectedIDExtension = value;
                NotifyPropertyChanged();
            }
        }

        public CreateCatalogViewModel()
        {
            CategoryList = new List<string>()
            {
                AssetCategory.Characters.Value,
                AssetCategory.Decks.Value,
                AssetCategory.Griptapes.Value,
                AssetCategory.Hats.Value,
                AssetCategory.Maps.Value,
                AssetCategory.Meshes.Value,
                AssetCategory.Pants.Value,
                AssetCategory.Shirts.Value,
                AssetCategory.Shoes.Value,
                AssetCategory.Trucks.Value,
                AssetCategory.Wheels.Value
            };

            DownloadTypeList = new List<string>()
            {
                "Url",
                "Google Drive",
                "Mega"
            };

            SelectedIDExtension = IDExtensions[0];

            AssetList = new ObservableCollection<AssetViewModel>();
            CatalogName = "";
            ClearSelectedAsset();
        }

        public BoolWithMessage ImportCatalog(string pathToCatalog)
        {
            try
            {
                AssetCatalog catalog = JsonConvert.DeserializeObject<AssetCatalog>(File.ReadAllText(pathToCatalog));

                AssetList = new ObservableCollection<AssetViewModel>(catalog.Assets.Select(a => new AssetViewModel(a)).ToList());
                ClearSelectedAsset();

                CatalogName = catalog.Name;

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to import catalog: {e.Message}");
            }
        }

        public BoolWithMessage ExportCatalog(string savePath)
        {
            try
            {
                UpdateAsset(SelectedAsset); // ensure the currently selected asset is updated before writing to file

                AssetCatalog catalog = new AssetCatalog()
                {
                    Name = CatalogName,
                    Assets = AssetList.Select(a => a.Asset).ToList()
                };

                string fileContents = JsonConvert.SerializeObject(catalog, Formatting.Indented);
                File.WriteAllText(savePath, fileContents);

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to export catalog: {e.Message}");
            }
        }

        public void ClearSelectedAsset()
        {
            AssetToEdit = null;
            SelectedAssetAuthor = "";
            SelectedAssetCategory = "";
            SelectedAssetDescription = "";
            SelectedAssetDownloadType = "";
            SelectedAssetDownloadUrl = "";
            SelectedAssetID = "";
            SelectedAssetImageUrl = "";
            SelectedAssetName = "";
            SelectedAssetUpdatedDate = "";
            SelectedAssetVersion = "";
        }

        private BoolWithMessage UpdateAsset(AssetViewModel assetToUpdate)
        {
            if (assetToUpdate == null)
            {
                return BoolWithMessage.False("assetToUpdate is null");
            }

            string validationMsg = ValidateAssetInfo(assetToUpdate);

            if (!string.IsNullOrEmpty(validationMsg))
            {
                UpdatedAssetInvalid?.Invoke(validationMsg);
                return BoolWithMessage.False(validationMsg);
            }

            if (SelectedAssetDownloadType == "Url")
            {
                assetToUpdate.Asset.DownloadLink = AssetCatalog.FormatUrl(SelectedAssetDownloadUrl);
            }
            else if (SelectedAssetDownloadType == "Google Drive")
            {
                assetToUpdate.Asset.DownloadLink = $"rsmm://GDrive/{SelectedAssetDownloadUrl}";
            }
            else if (SelectedAssetDownloadType == "Mega")
            {
                assetToUpdate.Asset.DownloadLink = $"rsmm://MegaFile/{SelectedAssetDownloadUrl}";
            }
            assetToUpdate.Asset.ID = $"{SelectedAssetID}{SelectedIDExtension}";
            assetToUpdate.Asset.Name = SelectedAssetName;
            assetToUpdate.Asset.Author = SelectedAssetAuthor;
            assetToUpdate.Asset.Category = SelectedAssetCategory;
            assetToUpdate.Asset.Description = SelectedAssetDescription;
            assetToUpdate.Asset.PreviewImage = SelectedAssetImageUrl;

            double.TryParse(SelectedAssetVersion, out double version);

            if (version <= 0)
            {
                assetToUpdate.Asset.Version = 1;
            }
            else
            {
                assetToUpdate.Asset.Version = version;
            }

            DateTime.TryParse(SelectedAssetUpdatedDate, out DateTime updateDate);

            if (updateDate != DateTime.MinValue)
            {
                assetToUpdate.Asset.UpdatedDate = updateDate.ToUniversalTime();
            }
            else if (string.IsNullOrWhiteSpace(SelectedAssetUpdatedDate))
            {
                assetToUpdate.Asset.UpdatedDate = DateTime.UtcNow;
            }

            assetToUpdate.Name = assetToUpdate.Asset.Name;
            assetToUpdate.Author = assetToUpdate.Asset.Author;
            assetToUpdate.AssetCategory = assetToUpdate.Asset.Category;
            assetToUpdate.Version = assetToUpdate.Asset.Version.ToString();
            assetToUpdate.UpdatedDate_dt = assetToUpdate.Asset.UpdatedDate;
            assetToUpdate.Description = assetToUpdate.Asset.Description;

            return BoolWithMessage.True();
        }

        public void DeleteAsset(AssetViewModel selectedAsset)
        {
            if (selectedAsset == null)
            {
                return;
            }

            AssetList.Remove(selectedAsset);
        }

        public BoolWithMessage AddAsset()
        {
            string errorMessage = ValidateAssetInfo(null);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                return BoolWithMessage.False($"The following errors were found:\n\n{errorMessage}");
            }

            Asset newAsset = new Asset()
            {
                ID = $"{SelectedAssetID}{SelectedIDExtension}",
                Author = SelectedAssetAuthor,
                Category = SelectedAssetCategory,
                Description = SelectedAssetDescription,
                Name = SelectedAssetName,
                PreviewImage = SelectedAssetImageUrl,
                UpdatedDate = DateTime.UtcNow,
                Version = 1,
            };


            if (!string.IsNullOrWhiteSpace(SelectedAssetUpdatedDate))
            {
                DateTime.TryParse(SelectedAssetUpdatedDate, out DateTime updateDate);
                if (updateDate != null && updateDate != DateTime.MinValue)
                {
                    newAsset.UpdatedDate = updateDate;
                }
            }

            double.TryParse(SelectedAssetVersion, out double version);

            if (version <= 0)
            {
                newAsset.Version = version;
            }

            if (SelectedAssetDownloadType == "Url")
            {
                newAsset.DownloadLink = AssetCatalog.FormatUrl(SelectedAssetDownloadUrl);
            }
            else if (SelectedAssetDownloadType == "Google Drive")
            {
                newAsset.DownloadLink = $"rsmm://GDrive/{SelectedAssetDownloadUrl}";
            }
            else if (SelectedAssetDownloadType == "Mega")
            {
                newAsset.DownloadLink = $"rsmm://MegaFile/{SelectedAssetDownloadUrl}";
            }


            AssetViewModel viewModel = new AssetViewModel(newAsset);
            AssetList.Add(viewModel);

            AssetToEdit = null;
            SelectedAsset = null;
            SelectedAsset = AssetList[AssetList.Count - 1];

            return BoolWithMessage.True();
        }

        private string ValidateAssetInfo(AssetViewModel assetToExclude)
        {
            StringBuilder errorMessage = new StringBuilder();
            // validate required fields
            if (string.IsNullOrWhiteSpace(SelectedAssetID))
            {
                errorMessage.AppendLine("Asset ID is missing.");
            }

            if (Path.GetInvalidFileNameChars().Any(c => SelectedAssetID.Contains(c)))
            {
                errorMessage.AppendLine("Asset ID contains invalid characters.");
            }

            if (string.IsNullOrWhiteSpace(SelectedAssetName))
            {
                errorMessage.AppendLine("Name is missing.");
            }

            if (string.IsNullOrWhiteSpace(SelectedAssetAuthor))
            {
                errorMessage.AppendLine("Author is missing.");
            }

            if (string.IsNullOrWhiteSpace(SelectedAssetCategory))
            {
                errorMessage.AppendLine("Category is missing.");
            }

            if (string.IsNullOrWhiteSpace(SelectedAssetDownloadUrl))
            {
                errorMessage.AppendLine("Download Url or Google Drive ID is missing.");
            }

            if (string.IsNullOrWhiteSpace(SelectedAssetImageUrl))
            {
                errorMessage.AppendLine("Preview Image Url is missing.");
            }

            if (string.IsNullOrWhiteSpace(SelectedAssetVersion))
            {
                SelectedAssetVersion = "1";
            }

            // validate ID is unique
            if (AssetList.Any(a => a != assetToExclude && a.Asset.ID == $"{SelectedAssetID}{SelectedIDExtension}"))
            {
                errorMessage.AppendLine($"Asset ID {SelectedAssetID}{SelectedIDExtension} is already in use. Change the ID to be unique.");
            }

            return errorMessage.ToString();
        }
    }
}
