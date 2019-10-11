
using Ini.Net;
using SessionMapSwitcher.Classes;
using SessionMapSwitcher.UI;
using SessionMapSwitcher.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SessionMapSwitcher.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        #region Data Members And Properties

        private string _sessionPath;
        private string _userMessage;
        private string _currentlyLoadedMapName;
        private ThreadFriendlyObservableCollection<MapListItem> _availableMaps;
        private object collectionLock = new object();
        private MapListItem _firstLoadedMap;
        private readonly MapListItem _defaultSessionMap;
        private bool _inputControlsEnabled;
        private bool _showInvalidMaps;
        private string _gravityText;
        private string _objectCountText;
        private bool _skipMovieIsChecked;
        private UnpackUtils _unpackUtils;
        private EzPzPatcher _patcher;
        private OnlineImportViewModel ImportViewModel;


        public String SessionPathTextInput
        {
            get
            {
                return _sessionPath;
            }
            set
            {
                _sessionPath = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(LoadMapButtonText));
                NotifyPropertyChanged(nameof(IsReplaceTextureControlEnabled));
                NotifyPropertyChanged(nameof(IsProjectWatchControlEnabled));
            }
        }

        public ThreadFriendlyObservableCollection<MapListItem> AvailableMaps
        {
            get
            {
                if (_availableMaps == null)
                {
                    _availableMaps = new ThreadFriendlyObservableCollection<MapListItem>();
                    BindingOperations.EnableCollectionSynchronization(_availableMaps, collectionLock);
                }
                return _availableMaps;
            }
            set
            {
                _availableMaps = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(FilteredAvailableMaps));
            }
        }

        /// <summary>
        /// Filtered collection of the available maps for the UI to show/hide invalid maps
        /// </summary>
        public ICollectionView FilteredAvailableMaps
        {
            get
            {
                var source = CollectionViewSource.GetDefaultView(AvailableMaps);
                source.Filter = p => Filter((MapListItem)p);
                return source;
            }
        }

        /// <summary>
        /// Filter to hide maps when invalid and the option is unchecked
        /// </summary>
        private bool Filter(MapListItem p)
        {
            return ShowInvalidMapsIsChecked || (!ShowInvalidMapsIsChecked && p.IsValid && !p.IsHiddenByUser);
        }

        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                _userMessage = value;
                NotifyPropertyChanged();
            }
        }

        public string CurrentlyLoadedMapName
        {
            get
            {
                return _currentlyLoadedMapName;
            }
            set
            {
                _currentlyLoadedMapName = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Determine if all controls (buttons, textboxes) should be enabled or disabled in main window.
        /// </summary>
        public bool InputControlsEnabled
        {
            get => _inputControlsEnabled;
            set
            {
                _inputControlsEnabled = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsReplaceTextureControlEnabled));
                NotifyPropertyChanged(nameof(IsProjectWatchControlEnabled));
            }
        }

        public bool ShowInvalidMapsIsChecked
        {
            get => _showInvalidMaps;
            set
            {
                if (value != _showInvalidMaps)
                {
                    _showInvalidMaps = value;
                    NotifyPropertyChanged();
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.ShowInvalidMaps, value.ToString());
                }
            }
        }

        internal MapListItem FirstLoadedMap { get => _firstLoadedMap; set => _firstLoadedMap = value; }

        public string GravityText
        {
            get { return _gravityText; }
            set
            {
                _gravityText = value;
                NotifyPropertyChanged();
            }
        }

        public string ObjectCountText
        {
            get { return _objectCountText; }
            set
            {
                _objectCountText = value;
                NotifyPropertyChanged();
            }
        }

        public bool SkipMovieIsChecked
        {
            get { return _skipMovieIsChecked; }
            set
            {
                _skipMovieIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public string LoadMapButtonText
        {
            get
            {
                return "Load Map";
            }
        }

        public bool IsReplaceTextureControlEnabled
        {
            get
            {
                if (SessionPath.IsSessionPathValid())
                {
                    return InputControlsEnabled;
                }
                return false;
            }
        }

        public bool IsProjectWatchControlEnabled
        {
            get
            {
                if (SessionPath.IsSessionPathValid())
                {
                    return InputControlsEnabled;
                }
                return false;
            }
        }

        #endregion


        public MainWindowViewModel()
        {
            SessionPathTextInput = AppSettingsUtil.GetAppSetting(SettingKey.PathToSession);
            ShowInvalidMapsIsChecked = AppSettingsUtil.GetAppSetting(SettingKey.ShowInvalidMaps).Equals("true", StringComparison.OrdinalIgnoreCase);
            UserMessage = "";
            InputControlsEnabled = true;
            GravityText = "-980";
            ObjectCountText = "1000";

            _defaultSessionMap = new MapListItem()
            {
                FullPath = SessionPath.ToOriginalSessionMapFiles,
                MapName = "Session Default Map - Brooklyn Banks"
            };

            RefreshGameSettingsFromIniFiles();

            SetCurrentlyLoadedMap();

            GetObjectCountFromFile();
        }

        /// <summary>
        /// Sets <see cref="SessionPath.ToSession"/> and saves the value to appSettings in the applications .config file
        /// </summary>
        public void SetSessionPath(string pathToSession)
        {
            SessionPath.ToSession = pathToSession;
            SessionPathTextInput = pathToSession;
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.PathToSession, SessionPath.ToSession);
        }

        public bool LoadAvailableMaps()
        {
            lock (collectionLock)
            {
                AvailableMaps.Clear();
            }

            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = $"Cannot load available maps: 'Path To Session' has not been set.";
                return false;
            }

            if (Directory.Exists(SessionPath.ToContent) == false)
            {
                UserMessage = $"Cannot load available maps: {SessionPath.ToContent} does not exist. Make sure the Session Path is set correctly.";
                return false;
            }

            try
            {
                LoadAvailableMapsInSubDirectories(SessionPath.ToContent);
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load available maps: {e.Message}";
                return false;
            }

            MetaDataManager.SetCustomPropertiesForMaps(AvailableMaps);

            lock (collectionLock)
            {
                SelectCurrentlyLoadedMapInList();

                // sort the maps A -> Z
                AvailableMaps = new ThreadFriendlyObservableCollection<MapListItem>(AvailableMaps.OrderBy(m => m.DisplayName));
                BindingOperations.EnableCollectionSynchronization(AvailableMaps, collectionLock);

                // add default session map to select (add last so it is always at top of list)
                _defaultSessionMap.IsEnabled = IsOriginalMapFilesBackedUp();
                _defaultSessionMap.FullPath = SessionPath.ToOriginalSessionMapFiles;
                _defaultSessionMap.Tooltip = _defaultSessionMap.IsEnabled ? null : "The original Session game files have not been backed up to the custom Maps folder.";
                AvailableMaps.Insert(0, _defaultSessionMap);
            }


            UserMessage = "List of available maps loaded!";
            return true;
        }

        private void SelectCurrentlyLoadedMapInList()
        {
            MapListItem currentlyLoaded = AvailableMaps.Where(m => m.MapName == CurrentlyLoadedMapName).FirstOrDefault();

            if (currentlyLoaded != null)
            {
                foreach (MapListItem map in AvailableMaps)
                {
                    map.IsSelected = false;
                }
                currentlyLoaded.IsSelected = true;
            }
        }

        private void LoadAvailableMapsInSubDirectories(string dirToSearch)
        {
            // loop over files in given map directory and look for files with the '.umap' extension
            foreach (string file in Directory.GetFiles(dirToSearch))
            {
                if (file.EndsWith(".umap") == false)
                {
                    continue;
                }

                MapListItem mapItem = new MapListItem
                {
                    FullPath = file,
                    MapName = file.Replace(dirToSearch + "\\", "").Replace(".umap", "")
                };
                mapItem.Validate();

                if (mapItem.DirectoryPath.Contains(SessionPath.ToNYCFolder))
                {
                    // skip files that are known to be apart of the original map so they are not displayed in list of avaiable maps (like the NYC01_Persistent.umap file)
                    continue;
                }

                lock (collectionLock)
                {
                    if (IsMapAdded(mapItem) == false)
                    {
                        AvailableMaps.Add(mapItem);
                    }
                }
            }

            // recursively search for .umap files in sub directories (skipping 'Original_Session_Map')
            foreach (string subFolder in Directory.GetDirectories(dirToSearch))
            {
                DirectoryInfo info = new DirectoryInfo(subFolder);

                if (info.Name != SessionPath.MapBackupFolderName)
                {
                    LoadAvailableMapsInSubDirectories(subFolder);
                }
            }
        }

        internal void OpenComputerImportWindow()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Cannot import: You must set your path to Session before importing maps.";
                return;
            }

            ComputerImportViewModel importViewModel = new ComputerImportViewModel();

            ComputerImportWindow importWindow = new ComputerImportWindow(importViewModel)
            {
                WindowStyle = WindowStyle.ToolWindow,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            importWindow.ShowDialog();

            LoadAvailableMaps(); // reload list of available maps as it may have changed
        }

        internal void OpenOnlineImportWindow()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Cannot import: You must set your path to Session before importing maps.";
                return;
            }

            if (ImportViewModel == null)
            {
                // keep view model in memory for entire app so list of downloadable maps is cached (until app restart)
                ImportViewModel = new OnlineImportViewModel();
            }

            OnlineImportWindow importWindow = new OnlineImportWindow(ImportViewModel)
            {
                WindowStyle = WindowStyle.ToolWindow,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            importWindow.ShowDialog();

            LoadAvailableMaps(); // reload list of available maps as it may have changed
        }

        internal void ReimportMapFiles(MapListItem selectedItem)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Failed to re-import: Path To Session is invalid.";
                return;
            }

            ComputerImportViewModel importViewModel = new ComputerImportViewModel()
            {
                IsZipFileImport = false,
                PathInput = MetaDataManager.GetOriginalImportLocation(selectedItem.MapName)
            };

            UserMessage = "Re-importing in progress ...";

            importViewModel.ImportMapAsyncAndContinueWith(isReimport: true,
                (antecedent) =>
                {
                    if (antecedent.Result.Result)
                    {
                        UserMessage = "Map Re-imported Successfully!";
                        LoadAvailableMaps();
                    }
                    else
                    {
                        UserMessage = $"Failed to re-import map: {antecedent.Result.Message}";
                    }
                });
        }

        /// <summary>
        /// Opens a window to enter a new name for a map.
        /// Writes to meta data file if user clicks 'Rename' in window.
        /// </summary>
        /// <param name="selectedMap"></param>
        internal void OpenRenameMapWindow(MapListItem selectedMap)
        {
            RenameMapViewModel viewModel = new RenameMapViewModel(selectedMap);
            RenameMapWindow window = new RenameMapWindow(viewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            bool? result = window.ShowDialog();

            if (result.GetValueOrDefault(false) == true)
            {
                bool didWrite = MetaDataManager.WriteCustomMapPropertiesToFile(AvailableMaps);

                if (didWrite == false)
                {
                    UserMessage = "Failed to update .meta file with new custom name. Your custom name may have not been saved and will be lost when the app restarts.";
                    return;
                }

                LoadAvailableMaps();
                UserMessage = $"{selectedMap.MapName} renamed to {selectedMap.CustomName}!";
            }
        }

        private bool IsMapAdded(MapListItem map)
        {
            return AvailableMaps.Any(m => m.MapName == map.MapName && m.DirectoryPath == map.DirectoryPath);
        }

        internal bool BackupOriginalMapFiles()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Cannot backup: 'Path to Session' is invalid.";
                return false;
            }

            if (IsOriginalMapFilesBackedUp())
            {
                // the files are already backed up
                UserMessage = "Skipping backup: original files already backed up.";
                return false;
            }

            if (DoesOriginalMapFileExistInGameDirectory() == false)
            {
                // the original files are missing from the Session directory
                UserMessage = "Cannot backup: original map files for Session are missing from Session game directory.";
                return false;
            }

            try
            {
                // create folder if it doesn't exist
                Directory.CreateDirectory(SessionPath.ToOriginalSessionMapFiles);

                string fileNamePrefix = "NYC01_Persistent";
                string[] fileExtensionsToCheck = { ".umap", ".uexp", "_BuiltData.uasset", "_BuiltData.uexp", "_BuiltData.ubulk" };

                // copy NYC01_Persistent files to backup folder
                foreach (string fileExt in fileExtensionsToCheck)
                {
                    string fullPathToFile = $"{SessionPath.ToNYCFolder}\\{fileNamePrefix}{fileExt}";
                    string destFilePath = $"{SessionPath.ToOriginalSessionMapFiles}\\{fileNamePrefix}{fileExt}";
                    File.Copy(fullPathToFile, destFilePath, overwrite: true);
                }


            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to backup original map files: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            _defaultSessionMap.IsEnabled = IsOriginalMapFilesBackedUp();
            _defaultSessionMap.Tooltip = _defaultSessionMap.IsEnabled ? null : "The original Session game files have not been backed up to the custom Maps folder.";
            return true;
        }

        internal bool IsOriginalMapFilesBackedUp()
        {
            if (Directory.Exists(SessionPath.ToOriginalSessionMapFiles) == false)
            {
                return false;
            }

            string fileNamePrefix = "NYC01_Persistent";
            string[] fileExtensionsToCheck = { ".umap", ".uexp", "_BuiltData.uasset", "_BuiltData.uexp", "_BuiltData.ubulk" };

            // copy NYC01_Persistent files to backup folder
            foreach (string fileExt in fileExtensionsToCheck)
            {
                string fullPath = $"{SessionPath.ToOriginalSessionMapFiles}\\{fileNamePrefix}{fileExt}";
                if (File.Exists(fullPath) == false)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool DoesOriginalMapFileExistInGameDirectory()
        {
            if (Directory.Exists(SessionPath.ToNYCFolder) == false)
            {
                return false;
            }

            string fileNamePrefix = "NYC01_Persistent";
            string[] fileExtensionsToCheck = { ".umap", ".uexp", "_BuiltData.uasset", "_BuiltData.uexp", "_BuiltData.ubulk" };

            // copy NYC01_Persistent files to backup folder
            foreach (string fileExt in fileExtensionsToCheck)
            {
                string fullPath = $"{SessionPath.ToNYCFolder}\\{fileNamePrefix}{fileExt}";
                if (File.Exists(fullPath) == false)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool IsSessionRunning()
        {
            var allProcs = Process.GetProcessesByName("SessionGame-Win64-Shipping");

            return allProcs.Length > 0;
        }

        private bool CopyMapFilesToNYCFolder(MapListItem map)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            // copy all files related to map to game directory
            foreach (string fileName in Directory.GetFiles(map.DirectoryPath))
            {
                if (fileName.Contains(map.MapName))
                {
                    FileInfo fi = new FileInfo(fileName);
                    string fullTargetFilePath = SessionPath.ToNYCFolder;

                    fullTargetFilePath += $"\\NYC01_Persistent";

                    if (fileName.Contains("_BuiltData"))
                    {
                        fullTargetFilePath += $"_BuiltData{fi.Extension}";
                    }
                    else
                    {
                        fullTargetFilePath += fi.Extension;
                    }

                    File.Copy(fileName, fullTargetFilePath, overwrite: true);
                }
            }

            return true;
        }

        internal void LoadMap(string mapName)
        {
            LoadAvailableMaps();

            foreach (var map in AvailableMaps)
            {
                if (map.MapName == mapName)
                {
                    LoadMap(map);
                    return;
                }
            }
            UserMessage = $"Cannot find map with name {mapName}!";
        }

        internal void ToggleVisiblityOfMap(MapListItem map)
        {
            map.IsHiddenByUser = !map.IsHiddenByUser;

            bool didWrite = MetaDataManager.WriteCustomMapPropertiesToFile(AvailableMaps);

            if (didWrite == false)
            {
                UserMessage = "Failed to update .meta file. Map may have not have been hidden.";
                return;
            }

            LoadAvailableMaps();

            string word = map.IsHiddenByUser ? "hidden" : "visible";
            UserMessage = $"{map.DisplayName} is now {word}!";
        }

        internal void LoadMap(MapListItem map)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Cannot Load: 'Path to Session' is invalid.";
                return;
            }

            if (IsSessionRunning() == false || FirstLoadedMap == null)
            {
                FirstLoadedMap = map;
            }

            foreach (MapListItem availableMap in AvailableMaps)
            {
                availableMap.IsSelected = false;
            }
            map.IsSelected = true;

            if (map == _defaultSessionMap)
            {
                LoadOriginalMap();
                return;
            }

            try
            {
                // delete session map file / custom maps from game 
                DeleteAllMapFilesFromNYCFolder();

                CopyMapFilesToNYCFolder(map);

                // update the ini file with the new map path
                string selectedMapPath = "/Game/Art/Env/NYC/NYC01_Persistent";
                UpdateGameDefaultMapIniSetting(selectedMapPath);

                SetCurrentlyLoadedMap();

                UserMessage = $"{map.MapName} Loaded!";
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load {map.MapName}: {e.Message}";
            }
        }

        internal void LoadOriginalMap()
        {
            try
            {
                DeleteAllMapFilesFromNYCFolder();

                UpdateGameDefaultMapIniSetting("/Game/Tutorial/Intro/MAP_EntryPoint");

                SetCurrentlyLoadedMap();

                UserMessage = $"{_defaultSessionMap.MapName} Loaded!";

            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load Original Session Game Map : {e.Message}";
            }
        }

        private static void CopyOriginalMapFilesToNYCFolder()
        {
            string fileNamePrefix = "NYC01_Persistent";
            string[] fileExtensions = { ".umap", ".uexp", "_BuiltData.uasset", "_BuiltData.uexp", "_BuiltData.ubulk" };

            // copy NYC01_Persistent backup files back to original game location
            foreach (string fileExt in fileExtensions)
            {
                string fullPath = $"{SessionPath.ToOriginalSessionMapFiles}\\{fileNamePrefix}{fileExt}";
                string targetPath = $"{SessionPath.ToNYCFolder}\\{fileNamePrefix}{fileExt}";
                File.Copy(fullPath, targetPath, true);
            }
        }

        private bool UpdateGameDefaultMapIniSetting(string defaultMapValue)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            IniFile iniFile = new IniFile(SessionPath.ToUserEngineIniFile);
            return iniFile.WriteString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap", defaultMapValue);
        }


        private string GetGameDefaultMapIniSetting()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return "";
            }

            try
            {
                IniFile iniFile = new IniFile(SessionPath.ToDefaultEngineIniFile);
                return iniFile.ReadString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap");
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Deletes all files in the Content\Art\Env\NYC folder
        /// </summary>
        private void DeleteAllMapFilesFromNYCFolder()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return;
            }

            foreach (string fileName in Directory.GetFiles(SessionPath.ToNYCFolder))
            {
                File.Delete(fileName);
            }
        }

        internal void SetCurrentlyLoadedMap()
        {
            string iniValue = GetGameDefaultMapIniSetting();

            if (iniValue == "/Game/Tutorial/Intro/MAP_EntryPoint.MAP_EntryPoint")
            {
                CurrentlyLoadedMapName = _defaultSessionMap.MapName;
            }
            else if (String.IsNullOrEmpty(iniValue) == false)
            {
                int startIndex = iniValue.LastIndexOf("/") + 1;

                if (startIndex >= 0)
                {
                    CurrentlyLoadedMapName = iniValue.Substring(startIndex, iniValue.Length - startIndex);
                }
                else
                {
                    CurrentlyLoadedMapName = "Unknown.";
                }
            }
        }

        internal void OpenFolderToSession()
        {
            try
            {
                Process.Start(SessionPath.ToSession);
            }
            catch (Exception ex)
            {
                UserMessage = $"Cannot open folder: {ex.Message}";
            }
        }

        internal void OpenFolderToSessionContent()
        {
            try
            {
                Process.Start(SessionPath.ToContent);
            }
            catch (Exception ex)
            {
                UserMessage = $"Cannot open folder: {ex.Message}";
            }
        }

        internal void OpenFolderToSelectedMap(MapListItem map)
        {
            try
            {
                Process.Start(map.DirectoryPath);
            }
            catch (Exception ex)
            {
                UserMessage = $"Cannot open folder: {ex.Message}";
            }
        }

        public void RefreshGameSettingsFromIniFiles()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return;
            }

            try
            {
                IniFile engineFile = new IniFile(SessionPath.ToDefaultEngineIniFile);
                GravityText = engineFile.ReadString("/Script/Engine.PhysicsSettings", "DefaultGravityZ");

                if (String.IsNullOrWhiteSpace(GravityText))
                {
                    GravityText = "-980";
                }

                IniFile gameFile = new IniFile(SessionPath.ToDefaultGameIniFile);
                SkipMovieIsChecked = gameFile.ReadBoolean("/Script/UnrealEd.ProjectPackagingSettings", "bSkipMovies");
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to get game settings: {e.Message}";
                GravityText = "-980";
                SkipMovieIsChecked = true;
            }
        }

        /// <summary>
        /// writes the game settings to the correct files.
        /// </summary>
        /// <returns> true if settings updated; false otherwise. </returns>
        public bool WriteGameSettingsToFile()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            // remove trailing 0's from float value for it to parse correctly
            int indexOfDot = GravityText.IndexOf(".");
            if (indexOfDot >= 0)
            {
                GravityText = GravityText.Substring(0, indexOfDot);
            }

            if (float.TryParse(GravityText, out _) == false)
            {
                UserMessage = "Invalid Gravity setting.";
                return false;
            }

            if (int.TryParse(ObjectCountText, out int parsedObjCount) == false)
            {
                UserMessage = "Invalid Object Count setting.";
                return false;
            }

            if (parsedObjCount <= 0 || parsedObjCount > 65535)
            {
                UserMessage = "Object Count must be between 0 and 65535.";
                return false;
            }

            SetObjectCountInFile();

            try
            {
                IniFile engineFile = new IniFile(SessionPath.ToDefaultEngineIniFile);
                engineFile.WriteString("/Script/Engine.PhysicsSettings", "DefaultGravityZ", GravityText);

                IniFile gameFile = new IniFile(SessionPath.ToDefaultGameIniFile);

                if (SkipMovieIsChecked)
                {
                    // delete the two StartupMovies from .ini
                    if (gameFile.KeyExists("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies"))
                    {
                        gameFile.DeleteKey("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies");
                    }
                    if (gameFile.KeyExists("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies"))
                    {
                        gameFile.DeleteKey("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies");
                    }
                }
                else
                {
                    if (gameFile.KeyExists("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies") == false)
                    {
                        gameFile.WriteString("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies", "UE4_Moving_Logo_720\n+StartupMovies=IntroLOGO_720_30");
                    }
                }

                gameFile.WriteString("/Script/UnrealEd.ProjectPackagingSettings", "bSkipMovies", SkipMovieIsChecked.ToString());
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to update game settings: {e.Message}";
                return false;
            }



            return true;
        }


        /// <summary>
        /// Get the Object Placement count from the file (only reads the first address) and set <see cref="ObjectCountText"/>
        /// </summary>
        internal void GetObjectCountFromFile()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return;
            }

            try
            {
                string objectFilePath = $"{SessionPath.ToSession}\\SessionGame\\Content\\ObjectPlacement\\Blueprints\\PBP_ObjectPlacementInventory.uexp";
                using (var stream = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = 351;
                    int byte1 = stream.ReadByte();
                    int byte2 = stream.ReadByte();
                    byte[] byteArray;

                    // convert two bytes to a hex string. if the second byte is less than 16 than swap the bytes due to reasons....
                    if (byte2 == 0)
                    {
                        byteArray = new byte[] { 0x00, Byte.Parse(byte1.ToString()) };
                    }
                    else if (byte2 < 16)
                    {
                        byteArray = new byte[] { Byte.Parse(byte2.ToString()), Byte.Parse(byte1.ToString()) };
                    }
                    else
                    {
                        byteArray = new byte[] { Byte.Parse(byte1.ToString()), Byte.Parse(byte2.ToString()) };
                    }
                    string hexString = BitConverter.ToString(byteArray).Replace("-", "");

                    // convert the hex string to base 10 int value
                    int intAgain = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                    ObjectCountText = intAgain.ToString();
                }
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to get object count: {e.Message}";
            }
        }

        /// <summary>
        /// Updates the PBP_ObjectPlacementInventory.uexp file with the new object count value (every placeable object is updated with new count).
        /// This works by converting <see cref="ObjectCountText"/> to bytes and writing the bytes to specific addresses in the file.
        /// </summary>
        internal void SetObjectCountInFile()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return;
            }

            string objectFilePath = $"{SessionPath.ToSession}\\SessionGame\\Content\\ObjectPlacement\\Blueprints\\PBP_ObjectPlacementInventory.uexp";

            // this is a list of addresses where the item count for placeable objects are stored in the .uexp file
            // ... if this file is modified then these addresses will NOT match so it is important to not mod/change the PBP_ObjectPlacementInventory file (until further notice...)
            List<int> addresses = new List<int>() { 351, 615, 681, 747, 879, 945, 1011, 1077, 1143, 1209, 1275, 1341, 1407, 1473, 1605 };

            try
            {
                using (var stream = new FileStream(objectFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // convert the base 10 int into a hex string (e.g. 10 => 'A' or 65535 => 'FF')
                    string hexValue = int.Parse(ObjectCountText).ToString("X");

                    // convert the hext string into a byte array that will be written to the file
                    byte[] bytes = StringToByteArray(hexValue);

                    if (hexValue.Length == 3)
                    {
                        // swap bytes around for some reason when the hex string is only 3 characters long... big-endian little-endian??
                        byte temp = bytes[1];
                        bytes[1] = bytes[0];
                        bytes[0] = temp;
                    }

                    // loop over every address so every placeable object is updated with new item count
                    foreach (int fileAddress in addresses)
                    {
                        stream.Position = fileAddress;
                        stream.WriteByte(bytes[0]);

                        // when object count is less than 16 than the byte array will only have 1 byte so write null in next byte position
                        if (bytes.Length > 1)
                        {
                            stream.WriteByte(bytes[1]);
                        }
                        else
                        {
                            stream.WriteByte(0x00);
                        }
                    }

                    stream.Flush(); // ensure file is written to
                }
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to set object count: {e.Message}";
            }
        }

        private static byte[] StringToByteArray(String hex)
        {
            // reference: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa

            if (hex.Length % 2 != 0)
            {
                // pad with '0' for odd length strings like 'A' so it becomes '0A' or '1A4' => '01A4'
                hex = '0' + hex;
            }

            int numChars = hex.Length;
            byte[] bytes = new byte[numChars / 2];
            for (int i = 0; i < numChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        internal void StartUnpacking()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Cannot unpack: Set Path to Session before unpacking game.";
                return;
            }

            if (App.IsRunningAppAsAdministrator() == false)
            {
                MessageBoxResult result = MessageBox.Show($"{App.GetAppName()} is not running as Administrator. This can lead to the unpacking process failing to copy files.\n\nDo you want to restart the program as Administrator?",
                                                           "Warning!",
                                                           MessageBoxButton.YesNo,
                                                           MessageBoxImage.Warning,
                                                           MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    App.RestartAsAdminstrator();
                    return;
                }
            }

            // rename the .bak file back to .pak so the user can easily force the game to be unpacked again
            // ... if a .bak file and a .pak file exist together then the .pak file will be used for unpacking and .bak file will be deleted at the end
            string bakFileName = SessionPath.ToPakFile.Replace(".pak", ".bak");

            if (File.Exists(bakFileName) && File.Exists(SessionPath.ToPakFile) == false)
            {
                File.Move(bakFileName, SessionPath.ToPakFile);
            }

            _unpackUtils = new UnpackUtils();

            _unpackUtils.ProgressChanged += UnpackOrPatch_ProgressChanged;
            _unpackUtils.UnpackCompleted += UnpackUtils_UnpackCompleted;

            InputControlsEnabled = false;
            _unpackUtils.StartUnpackingAsync(SessionPath.ToSession);
        }

        private void UnpackUtils_UnpackCompleted(bool wasSuccessful)
        {
            _unpackUtils.ProgressChanged -= UnpackOrPatch_ProgressChanged;
            _unpackUtils.UnpackCompleted -= UnpackUtils_UnpackCompleted;
            _unpackUtils = null;

            if (wasSuccessful)
            {
                // confirm game unpacked
                if (UnpackUtils.IsSessionUnpacked())
                {
                    BackupOriginalMapFiles();
                }

                UserMessage = "Unpacking complete! You should now be able to play custom maps. Click 'Reload Available Maps' to see list of available maps (some maps were left by the devs of Session).";
            }

            InputControlsEnabled = true;
        }

        private void UnpackOrPatch_ProgressChanged(string message)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                UserMessage = message;
            }, System.Windows.Threading.DispatcherPriority.Background);
        }


        internal void PromptToPatch()
        {
            MessageBoxResult result = MessageBox.Show("This will download the required files to patch Session. This is needed after updating Session to a new version.\n\nAre you sure you want to continue?",
                                                      "Notice!",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Warning,
                                                      MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                StartPatching();
            }
        }

        internal void StartPatching()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Cannot patch: Set Path to Session before patching game.";
                return;
            }

            if (App.IsRunningAppAsAdministrator() == false)
            {
                MessageBoxResult result = MessageBox.Show($"{App.GetAppName()} is not running as Administrator. This can lead to the patching process failing to copy files.\n\nDo you want to restart the program as Administrator?",
                                                           "Warning!",
                                                           MessageBoxButton.YesNo,
                                                           MessageBoxImage.Warning,
                                                           MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    App.RestartAsAdminstrator();
                    return;
                }
            }

            _patcher = new EzPzPatcher();

            _patcher.ProgressChanged += UnpackOrPatch_ProgressChanged;
            _patcher.PatchCompleted += EzPzPatcher_PatchCompleted;

            InputControlsEnabled = false;
            _patcher.StartPatchingAsync(SessionPath.ToSession);
        }

        private void EzPzPatcher_PatchCompleted(bool wasSuccessful)
        {
            _patcher.ProgressChanged -= UnpackOrPatch_ProgressChanged;
            _patcher.PatchCompleted -= EzPzPatcher_PatchCompleted;
            _patcher = null;

            if (wasSuccessful)
            {
                BackupOriginalMapFiles();

                UserMessage = "Patching complete! You should now be able to play custom maps and replace textures.";
            }

            InputControlsEnabled = true;
        }
    }

}
