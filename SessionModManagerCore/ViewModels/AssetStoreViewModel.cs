using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.ViewModels
{
    public class AssetStoreViewModel : ViewModelBase
    {
        #region Fields

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string storeFolderName = "store_data";

        public const string downloadsFolderName = "temp_downloads";

        public const string thumbnailFolderName = "thumbnails";

        public const string settingsFileName = "catalogSettings.json";

        public const string catalogFileName = "catalog.json";


        public const string defaultInstallStatusValue = "Installed / Not Installed";
        public const string defaultAuthorValue = "Show All";

        private string _installButtonText;
        private string _removeButtonText;
        private Stream _imageSource;
        private string _selectedDescription;
        private string _selectedAuthor;
        private AuthorDropdownViewModel _authorToFilterBy;
        private string _selectedInstallStatus;
        private bool _deleteDownloadAfterInstall;
        private bool _isInstallButtonEnabled;
        private bool _isRemoveButtonEnabled;
        private bool _isInstallingAsset;
        private bool _isLoadingImage;
        private List<AssetViewModel> _filteredAssetList;
        private List<AssetViewModel> _allAssets;
        private List<AuthorDropdownViewModel> _authorList;
        private List<string> _installStatusList;
        private List<DownloadItemViewModel> _currentDownloads;
        private AssetCatalog _catalogCache;


        private object filteredListLock = new object();
        private object allListLock = new object();
        private object catalogCacheLock = new object();
        private object downloadListLock = new object();

        private bool _displayAll;
        private bool _displayMaps;
        private bool _displayDecks;
        private bool _displayGriptapes;
        private bool _displayTrucks;
        private bool _displayWheels;
        private bool _displayHats;
        private bool _displayShirts;
        private bool _displayPants;
        private bool _displayShoes;
        private bool _displayMeshes;
        private bool _displayCharacters;
        private string _searchText;
        private AssetViewModel _selectedAsset;
        private bool _isDownloadingAllImages;
        private string _previewImageText;

        #endregion

        #region Properties

        public static string AbsolutePathToStoreData
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, storeFolderName);
            }
        }

        public static string AbsolutePathToThumbnails
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, thumbnailFolderName);
            }
        }

        public static string AbsolutePathToTempDownloads
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, downloadsFolderName);
            }
        }

        public static string AbsolutePathToCatalogSettingsJson
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, settingsFileName);
            }
        }

        public static string AbsolutePathToCatalogJson
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, catalogFileName);
            }
        }


        public bool DisplayAll
        {
            get { return _displayAll; }
            set
            {
                _displayAll = value;

                // setting the private variables so the list is not refreshed for every category until the end
                _displayMaps = value;
                _displayDecks = value;
                _displayGriptapes = value;
                _displayTrucks = value;
                _displayWheels = value;
                _displayHats = value;
                _displayShirts = value;
                _displayPants = value;
                _displayShoes = value;
                _displayMeshes = value;
                _displayCharacters = value;

                RaisePropertyChangedEventsForCategories();
                RefreshFilteredAssetList();
                UpdateAppSettingsWithSelectedCategories();
            }
        }

        public bool DisplayMaps
        {
            get { return _displayMaps; }
            set
            {
                if (_displayMaps != value)
                {
                    _displayMaps = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreMapsChecked, DisplayMaps.ToString());
                }
            }
        }

        public bool DisplayDecks
        {
            get { return _displayDecks; }
            set
            {
                if (_displayDecks != value)
                {
                    _displayDecks = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreDecksChecked, DisplayDecks.ToString());
                }
            }
        }

        public bool DisplayGriptapes
        {
            get { return _displayGriptapes; }
            set
            {
                if (_displayGriptapes != value)
                {
                    _displayGriptapes = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreGriptapesChecked, DisplayGriptapes.ToString());
                }
            }
        }

        public bool DisplayTrucks
        {
            get { return _displayTrucks; }
            set
            {
                if (_displayTrucks != value)
                {
                    _displayTrucks = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreTrucksChecked, DisplayTrucks.ToString());
                }
            }
        }
        public bool DisplayWheels
        {
            get { return _displayWheels; }
            set
            {
                if (_displayWheels != value)
                {
                    _displayWheels = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreWheelsChecked, DisplayWheels.ToString());
                }
            }
        }

        public bool DisplayHats
        {
            get { return _displayHats; }
            set
            {
                if (_displayHats != value)
                {
                    _displayHats = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreHatsChecked, DisplayHats.ToString());
                }
            }
        }
        public bool DisplayShirts
        {
            get { return _displayShirts; }
            set
            {
                if (_displayShirts != value)
                {
                    _displayShirts = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShirtsChecked, DisplayShirts.ToString());
                }
            }
        }
        public bool DisplayPants
        {
            get { return _displayPants; }
            set
            {
                if (_displayPants != value)
                {
                    _displayPants = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStorePantsChecked, DisplayPants.ToString());
                }
            }
        }
        public bool DisplayShoes
        {
            get { return _displayShoes; }
            set
            {
                if (_displayShoes != value)
                {
                    _displayShoes = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShoesChecked, DisplayShoes.ToString());
                }
            }
        }

        public bool DisplayMeshes
        {
            get { return _displayMeshes; }
            set
            {
                if (_displayMeshes != value)
                {
                    _displayMeshes = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreMeshesChecked, DisplayMeshes.ToString());
                }
            }
        }

        public bool DisplayCharacters
        {
            get { return _displayCharacters; }
            set
            {
                if (_displayCharacters != value)
                {
                    _displayCharacters = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreCharactersChecked, DisplayCharacters.ToString());
                }
            }
        }

        public string InstallButtonText
        {
            get
            {
                if (String.IsNullOrEmpty(_installButtonText))
                    _installButtonText = "Install Asset";
                return _installButtonText;
            }
            set
            {
                _installButtonText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsInstallButtonEnabled
        {
            get
            {
                return _isInstallButtonEnabled;
            }
            set
            {
                _isInstallButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public string RemoveButtonText
        {
            get
            {
                if (String.IsNullOrEmpty(_removeButtonText))
                    _removeButtonText = "Remove Asset";
                return _removeButtonText;
            }
            set
            {
                _removeButtonText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsRemoveButtonEnabled
        {
            get
            {
                return _isRemoveButtonEnabled;
            }
            set
            {
                _isRemoveButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsInstallingAsset
        {
            get
            {
                return _isInstallingAsset;
            }
            set
            {
                _isInstallingAsset = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsRemoveButtonEnabled));
                NotifyPropertyChanged(nameof(IsInstallButtonEnabled));
            }
        }

        public bool DeleteDownloadAfterInstall
        {
            get { return _deleteDownloadAfterInstall; }
            set
            {
                if (_deleteDownloadAfterInstall != value)
                {
                    _deleteDownloadAfterInstall = value;
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.DeleteDownloadAfterAssetInstall, _deleteDownloadAfterInstall.ToString());
                }
            }
        }


        public bool IsLoadingImage
        {
            get
            {
                return _isLoadingImage;
            }
            set
            {
                _isLoadingImage = value;
                NotifyPropertyChanged();
            }
        }


        public Stream PreviewImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewImageText
        {
            get
            {
                return _previewImageText;
            }
            set
            {
                _previewImageText = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAuthor
        {
            get { return _selectedAuthor; }
            set
            {
                _selectedAuthor = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedDescription
        {
            get { return _selectedDescription; }
            set
            {
                _selectedDescription = value;
                NotifyPropertyChanged();
            }
        }

        public AuthorDropdownViewModel AuthorToFilterBy
        {
            get
            {
                if (_authorToFilterBy == null)
                    _authorToFilterBy = new AuthorDropdownViewModel(defaultAuthorValue, 0);

                return _authorToFilterBy;
            }
            set
            {
                if (_authorToFilterBy != value)
                {
                    _authorToFilterBy = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                }
            }
        }

        public string SelectedInstallStatus
        {
            get
            {
                if (String.IsNullOrEmpty(_selectedInstallStatus))
                    _selectedInstallStatus = "All";
                return _selectedInstallStatus;
            }
            set
            {
                if (_selectedInstallStatus != value)
                {
                    _selectedInstallStatus = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                }
            }
        }

        /// <summary>
        /// used to determine if list of available maps should be refreshed when switching back to the main window
        /// </summary>
        public bool HasDownloadedMap { get; set; }

        public List<AssetViewModel> AllAssets
        {
            get
            {
                if (_allAssets == null)
                    _allAssets = new List<AssetViewModel>();
                return _allAssets;
            }
            set
            {
                lock (allListLock)
                {
                    _allAssets = value;
                }
            }
        }

        public List<AssetViewModel> FilteredAssetList
        {
            get
            {
                if (_filteredAssetList == null)
                    _filteredAssetList = new List<AssetViewModel>();
                return _filteredAssetList;
            }
            set
            {
                lock (filteredListLock)
                {
                    _filteredAssetList = value;
                }
                NotifyPropertyChanged();
            }
        }

        public AssetViewModel SelectedAsset
        {
            get
            {
                return _selectedAsset;
            }
            set
            {
                if (_selectedAsset != value)
                {
                    _selectedAsset = value;
                    RefreshPreviewForSelected();
                    NotifyPropertyChanged();
                }
            }
        }

        public List<AuthorDropdownViewModel> AuthorList
        {
            get
            {
                if (_authorList == null)
                    _authorList = new List<AuthorDropdownViewModel>();

                return _authorList;
            }
            set
            {
                _authorList = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> InstallStatusList
        {
            get
            {
                if (_installStatusList == null)
                    _installStatusList = new List<string>();

                return _installStatusList;
            }
            set
            {
                _installStatusList = value;
                NotifyPropertyChanged();
            }
        }

        public List<DownloadItemViewModel> CurrentDownloads
        {
            get
            {
                if (_currentDownloads == null)
                    _currentDownloads = new List<DownloadItemViewModel>();
                return _currentDownloads;
            }
            set
            {
                _currentDownloads = value;
                NotifyPropertyChanged();
            }
        }

        public string SearchText
        {
            get
            {
                if (_searchText == null)
                    _searchText = "";

                return _searchText;
            }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                }
            }
        }

        public bool IsDownloadingAllImages
        {
            get
            {
                return _isDownloadingAllImages;
            }
            set
            {
                _isDownloadingAllImages = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotDownloadingAllImages));
            }
        }

        public bool IsNotDownloadingAllImages
        {
            get
            {
                return !_isDownloadingAllImages;
            }
        }

        #endregion


        public AssetStoreViewModel()
        {
            IsInstallingAsset = false;
            IsDownloadingAllImages = false;
            DisplayMaps = true;
            SetSelectedCategoriesFromAppSettings();

            if (AppSettingsUtil.GetAppSetting(SettingKey.DeleteDownloadAfterAssetInstall) == "")
            {
                DeleteDownloadAfterInstall = true; // default to true if does not exist in app config
            }
            else
            {
                DeleteDownloadAfterInstall = AppSettingsUtil.GetAppSetting(SettingKey.DeleteDownloadAfterAssetInstall).Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            InstallStatusList = new List<string>() { defaultInstallStatusValue, "Installed", "Not Installed" };
            SelectedInstallStatus = defaultInstallStatusValue;

            AuthorList = new List<AuthorDropdownViewModel>() { new AuthorDropdownViewModel(defaultAuthorValue, 0) };
            AuthorToFilterBy = AuthorList[0];

            _catalogCache = GetCurrentCatalog();
            ReloadAllAssets();
            CheckForCatalogUpdatesAsync(clearCache: false);
        }

        public void RefreshFilteredAssetList()
        {
            List<AssetCategory> categories = GetSelectedCategories();
            List<AssetViewModel> newList = new List<AssetViewModel>();

            foreach (AssetCategory cat in categories)
            {
                newList.AddRange(GetAssetsByCategory(cat));
            }

            RefreshAuthorList();

            if (AuthorToFilterBy.Author != defaultAuthorValue)
            {
                newList = newList.Where(a => a.Author == AuthorToFilterBy.Author).ToList();
            }


            if (SelectedInstallStatus != defaultInstallStatusValue)
            {
                // read currently installed textures/map from files into memory so checking each asset is quicker
                InstalledTexturesMetaData installedTextures = MetaDataManager.LoadTextureMetaData();
                List<MapMetaData> installedMaps = MetaDataManager.GetAllMetaDataForMaps();

                switch (SelectedInstallStatus)
                {
                    case "Installed":
                        newList = newList.Where(a => IsAssetInstalled(a, installedMaps, installedTextures)).ToList();
                        break;

                    case "Not Installed":
                        newList = newList.Where(a => IsAssetInstalled(a, installedMaps, installedTextures) == false).ToList();
                        break;

                    default:
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length <= 2)
            {
                FilteredAssetList = newList.OrderByDescending(a => a.Asset.UpdatedDate).ToList();
            }
            else
            {
                FilteredAssetList = newList.Where(a => a.Name.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0 || a.Description.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0).OrderByDescending(a => a.Asset.UpdatedDate).ToList();
            }

            MessageService.Instance.ShowMessage("");
            if (FilteredAssetList.Count == 0)
            {
                if (GetSelectedCategories().Count() == 0)
                {
                    MessageService.Instance.ShowMessage("Check categories to view the list of downloadable assets ...");
                }
                else if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    MessageService.Instance.ShowMessage($"No results found for: {SearchText}");
                }
            }
        }

        /// <summary>
        /// Updates <see cref="AuthorList"/> with distinct sorted list of authors in <see cref="AllAssets"/>
        /// </summary>
        private void RefreshAuthorList()
        {
            List<AuthorDropdownViewModel> newAuthorList = new List<AuthorDropdownViewModel>();

            // use GroupBy to get count of assets per author
            foreach (IGrouping<string, AssetViewModel> author in AllAssets.GroupBy(a => a.Author))
            {
                newAuthorList.Add(new AuthorDropdownViewModel(author.Key, author.Count()));
            }

            newAuthorList = newAuthorList.OrderBy(a => a.Author).ToList();

            newAuthorList.Insert(0, new AuthorDropdownViewModel(defaultAuthorValue, 0));

            AuthorList = newAuthorList;

            //clear selection if selected author not in list
            if (AuthorList.Any(a => a.Author == AuthorToFilterBy.Author) == false)
            {
                AuthorToFilterBy = AuthorList[0];
            }
        }

        public void RefreshPreviewForSelected()
        {
            if (SelectedAsset == null)
            {
                return;
            }

            SelectedAuthor = SelectedAsset?.Author;
            SelectedDescription = SelectedAsset?.Description;

            bool isInstalled = IsSelectedAssetInstalled();

            IsInstallButtonEnabled = !isInstalled;
            IsRemoveButtonEnabled = isInstalled;

            RefreshInstallButtonText();
            GetSelectedPreviewImageAsync();
        }

        private bool IsSelectedAssetInstalled()
        {
            if (SelectedAsset == null)
            {
                return false;
            }

            // pass in null to read from json files to check if asset is installed
            return IsAssetInstalled(SelectedAsset, null, null);
        }

        private bool IsAssetInstalled(AssetViewModel asset, List<MapMetaData> mapMetaData, InstalledTexturesMetaData installedTextures)
        {
            if (asset.AssetCategory == AssetCategory.Maps.Value)
            {
                if (mapMetaData == null)
                {
                    mapMetaData = MetaDataManager.GetAllMetaDataForMaps();
                }

                return mapMetaData.Any(m => m.AssetName == asset.Asset.ID);
            }
            else
            {
                return MetaDataManager.GetTextureMetaDataByName(asset.Asset.ID, installedTextures) != null;
            }
        }

        /// <summary>
        /// Returns list of AssetViewModels by category that exist in <see cref="AllAssets"/>
        /// </summary>
        private IEnumerable<AssetViewModel> GetAssetsByCategory(AssetCategory cat)
        {
            IEnumerable<AssetViewModel> assetsInCategory = new List<AssetViewModel>();

            lock (allListLock)
            {
                assetsInCategory = AllAssets.Where(a => a.AssetCategory == cat.Value);
            }

            return assetsInCategory;
        }

        /// <summary>
        /// Returns list of <see cref="AssetCategory"/> that are set to true to display
        /// by checking <see cref="DisplayDecks"/>, <see cref="DisplayGriptapes"/>, etc.
        /// </summary>
        private List<AssetCategory> GetSelectedCategories()
        {
            List<AssetCategory> selectedCategories = new List<AssetCategory>();

            if (DisplayDecks)
            {
                selectedCategories.Add(AssetCategory.Decks);
            }
            if (DisplayGriptapes)
            {
                selectedCategories.Add(AssetCategory.Griptapes);
            }
            if (DisplayHats)
            {
                selectedCategories.Add(AssetCategory.Hats);
            }
            if (DisplayMaps)
            {
                selectedCategories.Add(AssetCategory.Maps);
            }
            if (DisplayPants)
            {
                selectedCategories.Add(AssetCategory.Pants);
            }
            if (DisplayShirts)
            {
                selectedCategories.Add(AssetCategory.Shirts);
            }
            if (DisplayShoes)
            {
                selectedCategories.Add(AssetCategory.Shoes);
            }
            if (DisplayTrucks)
            {
                selectedCategories.Add(AssetCategory.Trucks);
            }
            if (DisplayWheels)
            {
                selectedCategories.Add(AssetCategory.Wheels);
            }
            if (DisplayMeshes)
            {
                selectedCategories.Add(AssetCategory.Meshes);
            }
            if (DisplayCharacters)
            {
                selectedCategories.Add(AssetCategory.Characters);
            }

            return selectedCategories;
        }

        private void GetSelectedPreviewImageAsync()
        {
            if (SelectedAsset == null)
            {
                return;
            }

            IsLoadingImage = true;

            bool isDownloadingImage = false;

            Task t = Task.Factory.StartNew(() =>
            {
                CreateRequiredFolders();

                if (SelectedAsset == null || SelectedAsset.Asset == null)
                {
                    MessageService.Instance.ShowMessage("");

                    if (PreviewImageSource != null)
                    {
                        PreviewImageSource.Close();
                        PreviewImageSource = null;
                    }

                    return;
                }

                PreviewImageText = "Loading Image Preview ...";
                DownloadPreviewImage(SelectedAsset, refreshSelectedImagePreview: true);

                if (PreviewImageSource != null)
                {
                    PreviewImageSource.Close();
                    PreviewImageSource = null;
                }

                // ensure the preview image is NOT downloading before trying to open the filestream...
                string pathToThumbnail = Path.Combine(AbsolutePathToThumbnails, SelectedAsset.Asset.IDWithoutExtension);
                isDownloadingImage = CurrentDownloads.Any(d => d.SaveFilePath.Equals(pathToThumbnail));

                if (!isDownloadingImage)
                {
                    using (FileStream stream = File.Open(pathToThumbnail, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        PreviewImageSource = new MemoryStream();
                        stream.CopyTo(PreviewImageSource);
                    }

                    PreviewImageSource = new MemoryStream(File.ReadAllBytes(pathToThumbnail));
                }

            });

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    MessageService.Instance.ShowMessage("Failed to get preview image.");
                    PreviewImageSource = null;
                    PreviewImageText = "Failed to get preview ...";
                    Logger.Error(taskResult.Exception);
                }

                IsLoadingImage = isDownloadingImage;
            });

        }

        public Task DownloadAllPreviewImagesAsync()
        {
            if (IsDownloadingAllImages)
            {
                return null; // don't allow for multiple tasks to run (wait for the task to complete so all images that need to be fetched are added to download queue)
            }

            IsDownloadingAllImages = true;

            Task t = Task.Factory.StartNew(() =>
            {
                CreateRequiredFolders();

                foreach (AssetViewModel asset in FilteredAssetList.ToList())
                {
                    DownloadPreviewImage(asset);
                }
            });

            t.ContinueWith((result) =>
            {
                IsDownloadingAllImages = false;

                if (result.IsFaulted)
                {
                    Logger.Warn(result.Exception);
                }
            });

            return t;
        }

        /// <summary>
        /// Adds image to download queue if out of date, the url is different, or missing from cache.
        /// </summary>
        /// <param name="asset"></param>
        internal void DownloadPreviewImage(AssetViewModel asset, bool refreshSelectedImagePreview = false)
        {
            string pathToThumbnail = Path.Combine(AbsolutePathToThumbnails, asset.Asset.IDWithoutExtension);

            if (ImageCache.IsOutOfDate(pathToThumbnail) || ImageCache.IsSourceUrlDifferent(pathToThumbnail, asset.Asset.PreviewImage))
            {
                ImageCache.AddOrUpdate(pathToThumbnail, asset.Asset.PreviewImage);

                Guid downloadId = Guid.NewGuid();
                Action onCancel = () =>
                {
                    ImageCache.Remove(pathToThumbnail);
                    RemoveFromDownloads(downloadId);
                };

                Action<Exception> onError = ex =>
                {
                    Logger.Warn(ex);
                    PreviewImageText = "Failed to get preview ...";

                    ImageCache.Remove(pathToThumbnail);
                    RemoveFromDownloads(downloadId);
                };

                Action onComplete = () =>
                {
                    RemoveFromDownloads(downloadId);

                    if (refreshSelectedImagePreview)
                    {
                        GetSelectedPreviewImageAsync();
                    }
                };

                string formattedUrl = "rsmm://Url/" + asset.Asset.PreviewImage.Replace("://", "$");
                DownloadItemViewModel downloadItem = new DownloadItemViewModel()
                {
                    UniqueId = downloadId,
                    DownloadType = DownloadType.Image,
                    ItemName = $"Downloading preview image - {asset.Name}",
                    DownloadUrl = formattedUrl,
                    SaveFilePath = pathToThumbnail,
                    OnCancel = onCancel,
                    OnComplete = onComplete,
                    OnError = onError
                };

                AddToDownloads(downloadItem);
            }
        }

        public void RefreshInstallButtonText()
        {
            if (SelectedAsset == null)
            {
                InstallButtonText = "Install Asset";
                RemoveButtonText = "Remove Asset";
                return;
            }

            string assetCatName = SelectedAsset.AssetCategory;

            Dictionary<string, string> categoryToText = new Dictionary<string, string>();
            categoryToText.Add(AssetCategory.Decks.Value, "Deck");
            categoryToText.Add(AssetCategory.Griptapes.Value, "Griptape");
            categoryToText.Add(AssetCategory.Hats.Value, "Hat");
            categoryToText.Add(AssetCategory.Maps.Value, "Map");
            categoryToText.Add(AssetCategory.Pants.Value, "Pants");
            categoryToText.Add(AssetCategory.Shirts.Value, "Shirt");
            categoryToText.Add(AssetCategory.Shoes.Value, "Shoes");
            categoryToText.Add(AssetCategory.Trucks.Value, "Trucks");
            categoryToText.Add(AssetCategory.Wheels.Value, "Wheels");
            categoryToText.Add(AssetCategory.Meshes.Value, "Mesh");
            categoryToText.Add(AssetCategory.Characters.Value, "Character");

            if (categoryToText.ContainsKey(assetCatName))
            {
                InstallButtonText = $"Install {categoryToText[assetCatName]}";
                RemoveButtonText = $"Remove {categoryToText[assetCatName]}";
            }
            else
            {
                InstallButtonText = "Install Asset";
                RemoveButtonText = "Remove Asset";
            }
        }

        /// <summary>
        /// Main method for downloading and installing the selected asset asynchronously.
        /// </summary>
        public void DownloadSelectedAssetAsync()
        {
            CreateRequiredFolders();

            if (SessionPath.IsSessionPathValid() == false)
            {
                MessageService.Instance.ShowMessage($"Cannot install: 'Path To Session' has not been set.");
                return;
            }

            AssetViewModel assetToDownload = SelectedAsset; // get the selected asset currently in-case user selection changes while download occurs

            string pathToDownload = Path.Combine(AbsolutePathToTempDownloads, assetToDownload.Asset.ID);
            Guid downloadId = Guid.NewGuid();

            Action onComplete = () =>
            {
                IsInstallingAsset = true;

                RemoveFromDownloads(downloadId);

                MessageService.Instance.ShowMessage($"Installing asset: {assetToDownload.Name} ... ");
                Task installTask = Task.Factory.StartNew(() =>
                {
                    InstallDownloadedAsset(assetToDownload, pathToDownload);
                });

                installTask.ContinueWith((installResult) =>
                {
                    if (installResult.IsFaulted)
                    {
                        MessageService.Instance.ShowMessage($"Failed to install asset ...");
                        Logger.Error(installResult.Exception);
                        IsInstallingAsset = false;
                        return;
                    }

                    // lastly delete downloaded file
                    if (DeleteDownloadAfterInstall && File.Exists(pathToDownload))
                    {
                        File.Delete(pathToDownload);
                    }

                    IsInstallingAsset = false;

                    RefreshPreviewForSelected();

                    // refresh list if filtering by installed or uninstalled
                    if (SelectedInstallStatus != defaultInstallStatusValue)
                    {
                        RefreshFilteredAssetList();
                    }

                    if (assetToDownload.AssetCategory == AssetCategory.Maps.Value)
                    {
                        HasDownloadedMap = true;
                    }

                });

            };

            Action<Exception> onError = ex =>
            {
                Logger.Warn(ex);
                RemoveFromDownloads(downloadId);
                MessageService.Instance.ShowMessage($"Failed to download {assetToDownload.Name} - {ex.Message}");
            };

            Action onCancel = () =>
            {
                RemoveFromDownloads(downloadId);
                MessageService.Instance.ShowMessage($"Canceled downloading {assetToDownload.Name}");
            };

            DownloadItemViewModel downloadItem = new DownloadItemViewModel()
            {
                UniqueId = downloadId,
                DownloadType = DownloadType.Asset,
                ItemName = $"Downloading {assetToDownload.Name}",
                DownloadUrl = assetToDownload.Asset.DownloadLink,
                SaveFilePath = pathToDownload,
                OnCancel = onCancel,
                OnComplete = onComplete,
                OnError = onError,
            };

            AddToDownloads(downloadItem);
        }

        /// <summary>
        /// Logic for determining how to install downloaded asset. (maps and textures are installed differently)
        /// </summary>
        /// <param name="assetToInstall"> asset being installed </param>
        /// <param name="pathToDownload"> absolute path to the downloaded asset file </param>
        private void InstallDownloadedAsset(AssetViewModel assetToInstall, string pathToDownload)
        {
            if (assetToInstall.AssetCategory == AssetCategory.Maps.Value)
            {
                // import map
                MapImportViewModel importViewModel = new MapImportViewModel()
                {
                    IsZipFileImport = true,
                    PathInput = pathToDownload,
                    AssetToImport = assetToInstall.Asset
                };
                Task<BoolWithMessage> importTask = importViewModel.ImportMapAsync();
                importTask.Wait();

                if (importTask.Result.Result)
                {
                    MessageService.Instance.ShowMessage($"Successfully installed {assetToInstall.Name}!");
                }
                else
                {
                    MessageService.Instance.ShowMessage($"Failed to install {assetToInstall.Name}: {importTask.Result.Message}");
                    Logger.Warn($"install failed: {importTask.Result.Message}");
                }
            }
            else
            {
                // replace texture
                TextureReplacerViewModel replacerViewModel = new TextureReplacerViewModel()
                {
                    PathToFile = pathToDownload,
                    AssetToInstall = assetToInstall.Asset
                };
                replacerViewModel.MessageChanged += TextureReplacerViewModel_MessageChanged;
                replacerViewModel.ReplaceTextures();
                replacerViewModel.MessageChanged -= TextureReplacerViewModel_MessageChanged;
            }
        }

        private void TextureReplacerViewModel_MessageChanged(string message)
        {
            MessageService.Instance.ShowMessage(message);
        }

        public void CreateRequiredFolders()
        {
            Directory.CreateDirectory(AbsolutePathToStoreData);
            Directory.CreateDirectory(AbsolutePathToThumbnails);
            Directory.CreateDirectory(AbsolutePathToTempDownloads);
        }

        /// <summary>
        /// Deletes the selected asset files from Session folders
        /// </summary>
        public void RemoveSelectedAsset()
        {
            AssetViewModel assetToRemove = SelectedAsset;
            BoolWithMessage deleteResult = BoolWithMessage.False("");

            if (assetToRemove.AssetCategory == AssetCategory.Maps.Value)
            {
                MapMetaData mapToDelete = MetaDataManager.GetAllMetaDataForMaps()?.Where(m => m.AssetName == assetToRemove.Asset.ID).FirstOrDefault();

                if (mapToDelete == null)
                {
                    MessageService.Instance.ShowMessage("Failed to find meta data to delete map files ...");
                    return;
                }

                deleteResult = MetaDataManager.DeleteMapFiles(mapToDelete);
            }
            else
            {
                TextureMetaData textureToDelete = MetaDataManager.GetTextureMetaDataByName(assetToRemove.Asset.ID);

                if (textureToDelete == null)
                {
                    MessageService.Instance.ShowMessage($"Failed to find meta data to delete texture files for {assetToRemove.Asset.ID}...");
                    return;
                }

                deleteResult = MetaDataManager.DeleteTextureFiles(textureToDelete);
            }


            MessageService.Instance.ShowMessage(deleteResult.Message);

            if (deleteResult.Result)
            {
                RefreshPreviewForSelected();

                // refresh list if filtering by installed or uninstalled
                if (SelectedInstallStatus != defaultInstallStatusValue)
                {
                    RefreshFilteredAssetList();
                }
            }
        }

        private void UpdateAppSettingsWithSelectedCategories()
        {
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreDecksChecked, DisplayDecks.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreGriptapesChecked, DisplayGriptapes.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreHatsChecked, DisplayHats.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreMapsChecked, DisplayMaps.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStorePantsChecked, DisplayPants.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShirtsChecked, DisplayShirts.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShoesChecked, DisplayShoes.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreTrucksChecked, DisplayTrucks.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreWheelsChecked, DisplayWheels.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreMeshesChecked, DisplayMeshes.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreCharactersChecked, DisplayCharacters.ToString());
        }

        private void SetSelectedCategoriesFromAppSettings()
        {
            _displayDecks = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreDecksChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayGriptapes = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreGriptapesChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayHats = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreHatsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayMaps = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreMapsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayPants = AppSettingsUtil.GetAppSetting(SettingKey.AssetStorePantsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayShirts = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreShirtsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayShoes = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreShoesChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayTrucks = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreTrucksChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayWheels = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreWheelsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayMeshes = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreMeshesChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayCharacters = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreCharactersChecked).Equals("true", StringComparison.OrdinalIgnoreCase);

            _displayAll = DisplayDecks && DisplayGriptapes && DisplayHats && DisplayMaps && DisplayPants && DisplayShirts && DisplayShoes && DisplayTrucks && DisplayWheels && DisplayMeshes && DisplayCharacters;

            RaisePropertyChangedEventsForCategories();
            RefreshFilteredAssetList();
        }

        private void RaisePropertyChangedEventsForCategories()
        {
            NotifyPropertyChanged(nameof(DisplayAll));
            NotifyPropertyChanged(nameof(DisplayMaps));
            NotifyPropertyChanged(nameof(DisplayDecks));
            NotifyPropertyChanged(nameof(DisplayGriptapes));
            NotifyPropertyChanged(nameof(DisplayTrucks));
            NotifyPropertyChanged(nameof(DisplayWheels));
            NotifyPropertyChanged(nameof(DisplayHats));
            NotifyPropertyChanged(nameof(DisplayShirts));
            NotifyPropertyChanged(nameof(DisplayPants));
            NotifyPropertyChanged(nameof(DisplayShoes));
            NotifyPropertyChanged(nameof(DisplayMeshes));
            NotifyPropertyChanged(nameof(DisplayCharacters));
        }

        private void AddToDownloads(DownloadItemViewModel downloadItem)
        {
            if (downloadItem == null || downloadItem?.IsCanceled == true)
            {
                return;
            }

            var currentList = CurrentDownloads.ToList();

            // return if already added to download queue
            if (currentList.Any(d => d.SaveFilePath == downloadItem.SaveFilePath))
            {
                return;
            }

            currentList.Add(downloadItem);

            lock (downloadListLock)
            {
                CurrentDownloads = currentList;

                if (CurrentDownloads.Count == 1)
                {
                    // first item added to queue so start download
                    AssetDownloader.Instance.Download(downloadItem);
                }
                else if (CurrentDownloads.Count > 1 && CurrentDownloads[0].IsStarted && CurrentDownloads[0].DownloadType == DownloadType.Asset)
                {
                    // only start another download if no other image or catalog is already downloading
                    bool isOtherDownloadStarted = CurrentDownloads.Any(d => d.IsStarted && d.DownloadType != DownloadType.Asset && !d.IsCanceled);
                    DownloadItemViewModel nextDownload = CurrentDownloads.FirstOrDefault(d => !d.IsStarted && d.DownloadType != DownloadType.Asset && !d.IsCanceled);

                    if (!isOtherDownloadStarted && nextDownload != null)
                    {
                        AssetDownloader.Instance.Download(nextDownload);
                    }
                }
            }

        }

        private void RemoveFromDownloads(DownloadItemViewModel download)
        {
            if (download == null)
            {
                return;
            }

            var currentList = CurrentDownloads.ToList();
            currentList.Remove(download);

            lock (downloadListLock)
            {
                CurrentDownloads = currentList;

                // get next item in queue to start downloading
                if (CurrentDownloads.Count > 0)
                {
                    bool isDownloadingAsset = CurrentDownloads.Any(d => d.IsStarted && d.DownloadType == DownloadType.Asset);
                    int nextItemIndex = -1;

                    // if an asset is currently downloading then start the next image/catalog download in queue
                    if (isDownloadingAsset)
                    {
                        nextItemIndex = CurrentDownloads.FindIndex(d => !d.IsStarted && !d.IsCanceled && d.DownloadType != DownloadType.Asset);
                    }
                    else
                    {
                        nextItemIndex = CurrentDownloads.FindIndex(d => !d.IsStarted && !d.IsCanceled);
                    }

                    if (nextItemIndex >= 0)
                    {
                        AssetDownloader.Instance.Download(CurrentDownloads[nextItemIndex]);
                    }
                }
            }
        }

        private void RemoveFromDownloads(Guid downloadID)
        {
            DownloadItemViewModel toRemove = null;
            lock (downloadListLock)
            {
                toRemove = CurrentDownloads.FirstOrDefault(d => d.UniqueId == downloadID);
            }

            RemoveFromDownloads(toRemove);
        }

        public Task CheckForCatalogUpdatesAsync(bool clearCache = true)
        {
            object countLock = new object();

            Task t = Task.Factory.StartNew(() =>
            {
                string catFile = AbsolutePathToCatalogSettingsJson;

                Directory.CreateDirectory(AbsolutePathToTempDownloads);

                CatalogSettings currentSettings = new CatalogSettings();

                if (File.Exists(catFile))
                {
                    currentSettings = JsonConvert.DeserializeObject<CatalogSettings>(File.ReadAllText(catFile));
                    currentSettings.CatalogUrls.RemoveAll(s => string.IsNullOrWhiteSpace(s.Url));
                }
                else
                {
                    CatalogSettings.AddDefaults(currentSettings);
                }


                if (currentSettings.CatalogUrls.Count == 0)
                {
                    if (File.Exists(catFile))
                    {
                        File.Delete(catFile);
                    }

                    _catalogCache = new AssetCatalog();
                    ReloadAllAssets();
                    RefreshFilteredAssetList();
                    return;
                }

                if (clearCache)
                {
                    lock (catalogCacheLock)
                    {
                        _catalogCache = new AssetCatalog();
                    }
                }

                foreach (CatalogSubscription sub in currentSettings.CatalogUrls.Where(c => c.IsActive).ToArray())
                {
                    Logger.Info($"Checking catalog {sub.Url}");

                    string uniqueFileName = $"cattemp{Path.GetRandomFileName()}.xml"; // save temp catalog update to unique filename so multiple catalog updates can download async
                    string path = Path.Combine(AbsolutePathToTempDownloads, uniqueFileName);

                    Guid downloadId = Guid.NewGuid();

                    Action onCancel = () =>
                    {
                        RemoveFromDownloads(downloadId);
                    };

                    Action<Exception> onError = ex =>
                    {
                        Logger.Warn(ex);
                        RemoveFromDownloads(downloadId);
                    };

                    Action onComplete = () =>
                    {
                        RemoveFromDownloads(downloadId);

                        try
                        {
                            AssetCatalog c = JsonConvert.DeserializeObject<AssetCatalog>(File.ReadAllText(path));

                            lock (catalogCacheLock) // put a lock on the Catalog so multiple threads can only merge one at a time
                            {
                                _catalogCache = AssetCatalog.Merge(_catalogCache, c);
                                File.WriteAllText(AbsolutePathToCatalogJson, JsonConvert.SerializeObject(_catalogCache, Formatting.Indented));
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                        finally
                        {
                            // delete temp catalog
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }

                            ReloadAllAssets();
                            RefreshFilteredAssetList();
                        }
                    };

                    DownloadItemViewModel catalogDownload = new DownloadItemViewModel()
                    {
                        UniqueId = downloadId,
                        DownloadType = DownloadType.Catalog,
                        ItemName = $"Checking catalog {sub.Url}",
                        DownloadUrl = AssetCatalog.FormatUrl(sub.Url),
                        SaveFilePath = path,
                        OnComplete = onComplete,
                        OnError = onError,
                        OnCancel = onCancel
                    };

                    AddToDownloads(catalogDownload);
                }
            });

            return t;
        }

        private void ReloadAllAssets()
        {
            AllAssets = _catalogCache.Assets.Select(a => new AssetViewModel(a)).ToList();
        }

        internal AssetCatalog GetCurrentCatalog()
        {
            if (!File.Exists(AbsolutePathToCatalogJson))
            {
                return new AssetCatalog();
            }

            return JsonConvert.DeserializeObject<AssetCatalog>(File.ReadAllText(AbsolutePathToCatalogJson));
        }

        public void LaunchDownloadInBrowser()
        {
            if (SelectedAsset == null)
            {
                return;
            }

            if (AssetCatalog.TryParseDownloadUrl(SelectedAsset.Asset.DownloadLink, out DownloadLocationType type, out string url))
            {
                if (type == DownloadLocationType.GDrive)
                {
                    url = $"https://drive.google.com/file/d/{url}/view?usp=sharing";
                }
                else if (type == DownloadLocationType.MegaFile)
                {
                    url = $"https://mega.nz/file/{url}";
                }

                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        public void CancelAllDownloads()
        {
            // first mark all items as being canceled
            foreach (DownloadItemViewModel item in CurrentDownloads)
            {
                item.IsCanceled = true;
            }

            // then peform actual cancelling of download which will remove from CurrentDownloads (hence .ToList())
            foreach (DownloadItemViewModel item in CurrentDownloads.ToList())
            {
                CancelDownload(item);
            }
        }

        public void CancelDownload(DownloadItemViewModel item)
        {
            if (item == null)
            {
                return;
            }

            // if download has not started then PerformCancel (which calls OnCancel internall) will be null and we will need to invoke the OnCancel method instead
            if (item.PerformCancel != null)
            {
                item.PerformCancel.Invoke();
            }
            else
            {
                item.OnCancel?.Invoke();
            }
        }
    }
}
