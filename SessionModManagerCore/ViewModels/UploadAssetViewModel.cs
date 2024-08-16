using Newtonsoft.Json;
using SessionAssetStore;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class UploadAssetViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string DefaultStatusMesssage = "Enter the required info and click 'Upload Asset' to upload your asset to the Asset Store";

        private string _name;
        private string _author;
        private string _description;
        private string _pathToFile;
        private string _pathToThumbnail;
        private string _selectedCategory;
        private string _selectedBucketName;
        private string _statusMessage;
        private bool _isUploadingAsset;
        private List<string> _availableCategories;
        private List<string> _availableBuckets;


        private StorageManager _assetManager;

        public bool HasAuthenticated { get; set; }

        public string PathToCredentialsFile { get; set; }

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
                if (_author != value)
                {
                    _author = value;
                    NotifyPropertyChanged();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.UploaderAuthor, _author); // store author in app settings so it is remembered for future uploads
                }
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

        public string PathToFile
        {
            get { return _pathToFile; }
            set
            {
                _pathToFile = value;
                NotifyPropertyChanged();
            }
        }
        public string PathToThumbnail
        {
            get { return _pathToThumbnail; }
            set
            {
                _pathToThumbnail = value;
                NotifyPropertyChanged();
            }
        }
        public string SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                _selectedCategory = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedBucketName
        {
            get { return _selectedBucketName; }
            set
            {
                if (_selectedBucketName != value)
                {
                    _selectedBucketName = value;
                    NotifyPropertyChanged();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreSelectedBucket, _selectedBucketName);
                }
            }
        }
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                Logger.Info($"StatusMessage = {_statusMessage}");
                NotifyPropertyChanged();
            }
        }
        public bool IsUploadingAsset
        {
            get { return _isUploadingAsset; }
            set
            {
                _isUploadingAsset = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotUploadingAsset));
            }
        }

        public bool IsNotUploadingAsset
        {
            get { return !_isUploadingAsset; }
        }

        public List<string> AvailableCategories
        {
            get
            {
                if (_availableCategories == null)
                    _availableCategories = new List<string>();

                return _availableCategories;
            }
            set
            {
                _availableCategories = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> AvailableBuckets
        {
            get
            {
                if (_availableBuckets == null)
                    _availableBuckets = new List<string>();

                return _availableBuckets;
            }
            set
            {
                _availableBuckets = value;
                NotifyPropertyChanged();
            }
        }

        public StorageManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                    _assetManager = new StorageManager();

                return _assetManager;
            }
        }

        public UploadAssetViewModel()
        {
            IsUploadingAsset = false;
            HasAuthenticated = false;
            SelectedCategory = "";
            StatusMessage = DefaultStatusMesssage;
            PathToCredentialsFile = AppSettingsUtil.GetAppSetting(SettingKey.PathToCredentialsFile);
            Author = AppSettingsUtil.GetAppSetting(SettingKey.UploaderAuthor);
            SelectedBucketName = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreSelectedBucket);
            InitAvailableCategories();
        }

        private void InitAvailableCategories()
        {
            List<string> categories = new List<string>()
            {
                "Maps",
                "Decks",
                "Griptapes",
                "Trucks",
                "Wheels",
                "Hats",
                "Pants",
                "Shirts",
                "Shoes",
                "Meshes",
                "Characters"
            };

            AvailableCategories = categories;
        }

        public void SetPathToCredentialsJson(string fileName)
        {
            PathToCredentialsFile = fileName;
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.PathToCredentialsFile, PathToCredentialsFile);
        }

        public AssetCategory GetAssetCategoryBasedOnSelectedCategory()
        {
            switch (SelectedCategory)
            {
                case "Maps":
                    return AssetCategory.Maps;
                case "Decks":
                    return AssetCategory.Decks;
                case "Griptapes":
                    return AssetCategory.Griptapes;
                case "Trucks":
                    return AssetCategory.Trucks;
                case "Wheels":
                    return AssetCategory.Wheels;
                case "Hats":
                    return AssetCategory.Hats;
                case "Pants":
                    return AssetCategory.Pants;
                case "Shirts":
                    return AssetCategory.Shirts;
                case "Shoes":
                    return AssetCategory.Shoes;
                case "Meshes":
                    return AssetCategory.Meshes;
                case "Characters":
                    return AssetCategory.Characters;
                default:
                    return null;
            }
        }

        public void UploadAsset()
        {
            // Validate fields before uploading
            if (String.IsNullOrEmpty(Name))
            {
                StatusMessage = "Please provide a Name for the asset.";
                return;
            }

            if (String.IsNullOrEmpty(Author))
            {
                StatusMessage = "Please provide an Author for the asset.";
                return;
            }

            if (String.IsNullOrEmpty(SelectedCategory))
            {
                StatusMessage = "Please select an Asset Category first.";
                return;
            }

            if (File.Exists(PathToFile) == false)
            {
                StatusMessage = $"File does not exist at {PathToFile}.";
                return;
            }

            if (File.Exists(PathToThumbnail) == false)
            {
                StatusMessage = $"Thumbnail does not exist at {PathToThumbnail}.";
                return;
            }

            if (String.IsNullOrEmpty(SelectedBucketName))
            {
                StatusMessage = "Please select a Bucket first.";
                return;
            }


            IsUploadingAsset = true;

            Task uploadTask = Task.Factory.StartNew(() =>
            {
                if (String.IsNullOrEmpty(Description))
                {
                    // if description is not given then use name as description
                    Description = Name;
                }

                string fileName = Author + "_" + Path.GetFileName(PathToFile);
                string thumbnailName = $"{Author}_{Path.GetFileNameWithoutExtension(PathToFile)}{Path.GetExtension(PathToThumbnail)}"; // make sure thumbnail on the storage server has same name as json and file
                AssetCategory category = GetAssetCategoryBasedOnSelectedCategory();

                Asset assetToUpload = new Asset(Name, Description, Author, fileName, thumbnailName, category.Value, DateTime.UtcNow.ToString());

                // save asset .json to disk to upload
                string pathToTempJson = Path.Combine(AssetStoreViewModel.AbsolutePathToTempDownloads, $"{Author}_{Path.GetFileNameWithoutExtension(PathToFile)}.json");
                string jsonToSave = JsonConvert.SerializeObject(assetToUpload, Formatting.Indented);


                Directory.CreateDirectory(AssetStoreViewModel.AbsolutePathToTempDownloads);
                File.WriteAllText(pathToTempJson, jsonToSave);

                var task = AssetManager.UploadAssetAsync(pathToTempJson, PathToThumbnail, PathToFile, SelectedBucketName, new EventHandler<Amazon.S3.Transfer.UploadProgressArgs>((o,p) => StatusMessage = $"Uploading files ... {p.TransferredBytes / 1000000:0.00} / {p.TotalBytes / 1000000:0.00} MB  | {p.PercentDone}%")).ConfigureAwait(false);
                task.GetAwaiter().GetResult();

                File.Delete(pathToTempJson); // delete temp manifest after completion
            });

            uploadTask.ContinueWith((uploadResult) =>
            {
                IsUploadingAsset = false;

                if (uploadResult.IsFaulted)
                {
                    StatusMessage = $"An error occurred while uploading the files: {uploadResult.Exception.GetBaseException()?.Message}";
                    Logger.Error(uploadResult.Exception, "failed to upload asset");
                    return;
                }

                StatusMessage = $"The asset {Name} has been uploaded successfully! You can close this window or leave it open to upload another asset.";
            });


        }

        public void BrowseForFile()
        {
            throw new NotImplementedException();
        }

        public void BrowseForThumbnailFile()
        {
            throw new NotImplementedException();
        }

        public void TryAuthenticate()
        {
            if (File.Exists(PathToCredentialsFile) == false)
            {
                StatusMessage = $"Failed to authenticate to asset store: {PathToCredentialsFile} does not exist.";
                return;
            }

            try
            {
                AssetManager.Authenticate(PathToCredentialsFile);
                HasAuthenticated = true;
            }
            catch (AggregateException e)
            {
                StatusMessage = $"Failed to authenticate to asset store: {e.InnerException?.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
            catch (Exception e)
            {
                StatusMessage = $"Failed to authenticate to asset store: {e.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
        }

        /// <summary>
        /// sets <see cref="SelectedBucketName"/> to <see cref="Author"/> if the author is also a valid bucket name
        /// </summary>
        public void SetBucketBasedOnAuthor()
        {
            if (String.IsNullOrEmpty(SelectedBucketName) && AvailableBuckets.Any(b => b == Author))
            {
                SelectedBucketName = Author;
            }
        }
    }
}
