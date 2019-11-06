using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Classes.Interfaces;
using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SessionMapSwitcherCore.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Data Members And Properties

        private string _sessionPath;
        private string _userMessage;
        private string _hintMessage;
        private string _currentlyLoadedMapName;
        private List<MapListItem> _availableMaps;
        private object collectionLock = new object();
        private bool _inputControlsEnabled;
        private bool _showInvalidMaps;
        private string _gravityText;
        private string _objectCountText;
        private bool _skipMovieIsChecked;
        private EzPzPatcher _patcher;
        public OnlineImportViewModel ImportViewModel { get; set; }
        private IMapSwitcher MapSwitcher { get; set; }

        private readonly string[] _hintMessages = new string[] { "Right-click list of maps and click 'Open Content Folder ...' to get to the Content folder easily",
                                                                 "Download maps from online by clicking 'Import Map > From Online ...'",
                                                                 "Hide maps in the list by right-clicking the map and clicking 'Hide Selected Map ...'",
                                                                 "Rename maps in the list by right-clicking the map and clicking 'Rename Selected Map ...'",
                                                                 "Right-click anywhere and click 'View Help ...' to open the readme",
                                                                 "Use Project Watcher to auto-import your map after you cooked it in Unreal Engine",
                                                               };


        public string SessionPathTextInput
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
                NotifyPropertyChanged(nameof(IsPatchButtonEnabled));
                NotifyPropertyChanged(nameof(PatchButtonToolTip));
            }
        }

        public List<MapListItem> AvailableMaps
        {
            get
            {
                if (_availableMaps == null)
                {
                    _availableMaps = new List<MapListItem>();
                }
                return _availableMaps;
            }
            set
            {
                lock (collectionLock)
                {
                    _availableMaps = value;
                }
                RefreshFilteredMaps();
            }
        }

        public List<MapListItem> FilteredAvailableMaps
        {
            get
            {
                lock (collectionLock)
                {
                    return AvailableMaps.Where(m => Filter(m)).ToList();
                }
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

        public string HintMessage
        {
            get { return _hintMessage; }
            set
            {
                _hintMessage = value;
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
                NotifyPropertyChanged(nameof(IsPatchButtonEnabled));
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
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.ShowInvalidMaps, value.ToString());
                    NotifyPropertyChanged();
                    RefreshFilteredMaps();
                }
            }
        }

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

        public bool IsOriginalMapFilesBackedUp()
        {
            if (IsSessionUnpacked() == false)
            {
                return false;
            }

            return (MapSwitcher as UnpackedMapSwitcher).IsOriginalMapFilesBackedUp();
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

        public void BackupOriginalMapFiles()
        {
            if (IsSessionUnpacked() == false)
            {
                return;
            }

            BoolWithMessage backupResult = (MapSwitcher as UnpackedMapSwitcher).BackupOriginalMapFiles();

            if (backupResult.Result)
            {
                UserMessage = $"Original map files backed up to {SessionPath.ToOriginalSessionMapFiles}";
            }
            else
            {
                UserMessage = backupResult.Message;
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

        public bool IsPatchButtonEnabled
        {
            get
            {
                if (SessionPath.IsSessionPathValid() == false || UnpackUtils.IsSessionUnpacked())
                {
                    return false;
                }

                return InputControlsEnabled;
            }
        }

        public string PatchButtonToolTip
        {
            get
            {
                if (SessionPath.IsSessionPathValid() == false)
                {
                    return "Enter a valid path to Session.";
                }
                else if (UnpackUtils.IsSessionUnpacked())
                {
                    return "Game is already unpacked. You can not apply the patch for an unpacked game.";
                }

                return "Use this after updating Session to a new version or to patch the game again.";
            }
        }

        #endregion


        public MainWindowViewModel()
        {
            SessionPathTextInput = SessionPath.ToSession;
            ShowInvalidMapsIsChecked = AppSettingsUtil.GetAppSetting(SettingKey.ShowInvalidMaps).Equals("true", StringComparison.OrdinalIgnoreCase);
            UserMessage = "";
            InputControlsEnabled = true;
            GravityText = "-980";
            ObjectCountText = "1000";

            SetRandomHintMessage();

            if (EzPzPatcher.IsGamePatched())
            {
                MapSwitcher = new EzPzMapSwitcher();
            }
            else if (UnpackUtils.IsSessionUnpacked())
            {
                MapSwitcher = new UnpackedMapSwitcher();
            }

            if (MapSwitcher != null)
            {
                RefreshGameSettings();
                SetCurrentlyLoadedMap();
            }

        }

        public void RefreshGameSettings()
        {
            BoolWithMessage result = GameSettingsManager.RefreshGameSettingsFromIniFiles();

            if (result.Result == false)
            {
                UserMessage = result.Message;
            }

            ObjectCountText = GameSettingsManager.ObjectCount.ToString();
            GravityText = GameSettingsManager.Gravity.ToString();
            SkipMovieIsChecked = GameSettingsManager.SkipIntroMovie;
        }

        public bool UpdateGameSettings()
        {
            string concatenatedErrorMsg = "";

            if (GameSettingsManager.DoesObjectPlacementFileExist() == false)
            {
                concatenatedErrorMsg = "Custom object count will not be applied until required file is extracted; ";
            }

            InputControlsEnabled = false;

            BoolWithMessage didSetSettings = GameSettingsManager.ValidateAndUpdateGravityAndSkipMoviesSettings(GravityText, SkipMovieIsChecked);
            BoolWithMessage didSetObjCount = BoolWithMessage.True(); // set to true by default in case the user does not have the file to modify


            if (GameSettingsManager.DoesObjectPlacementFileExist())
            {
                didSetObjCount = GameSettingsManager.ValidateAndUpdateObjectCount(ObjectCountText);

                if (didSetObjCount.Result == false)
                {
                    concatenatedErrorMsg += didSetObjCount.Message;
                }
            }

            if (didSetSettings.Result == false)
            {
                concatenatedErrorMsg += didSetSettings.Message;
            }

            if (String.IsNullOrEmpty(concatenatedErrorMsg) == false)
            {
                UserMessage = concatenatedErrorMsg;
            }

            InputControlsEnabled = true;
            return didSetSettings.Result || didSetObjCount.Result;
        }

        /// <summary>
        /// Sets <see cref="SessionPath.ToSession"/> and saves the value to appSettings in the applications .config file
        /// </summary>
        public void SetSessionPath(string pathToSession)
        {
            SessionPath.ToSession = pathToSession;
            SessionPathTextInput = pathToSession;
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.PathToSession, SessionPath.ToSession);

            if (UnpackUtils.IsSessionUnpacked())
            {
                MapSwitcher = new UnpackedMapSwitcher();
            }
            else if (EzPzPatcher.IsGamePatched())
            {
                MapSwitcher = new EzPzMapSwitcher();
            }
            else
            {
                MapSwitcher = null;
            }
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
                AvailableMaps = new List<MapListItem>(AvailableMaps.OrderBy(m => m.DisplayName));

                AddDefaultMapToAvailableMaps();
            }


            UserMessage = "List of available maps loaded!";
            return true;
        }

        /// <summary>
        /// adds the default session map to beginning of list of <see cref="AvailableMaps"/>
        /// </summary>
        private void AddDefaultMapToAvailableMaps()
        {
            if (MapSwitcher == null || AvailableMaps == null)
            {
                return;
            }

            MapSwitcher.GetDefaultSessionMap().FullPath = SessionPath.ToOriginalSessionMapFiles;
            AvailableMaps.Insert(0, MapSwitcher.GetDefaultSessionMap());
            NotifyPropertyChanged(nameof(AvailableMaps));
            NotifyPropertyChanged(nameof(FilteredAvailableMaps));
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

                FileInfo mapFileInfo = new FileInfo(file);

                MapListItem mapItem = new MapListItem
                {
                    FullPath = file,
                    MapName = mapFileInfo.NameWithoutExtension()
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

        public void ReimportMapFiles(MapListItem selectedItem)
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

        private bool IsMapAdded(MapListItem map)
        {
            return AvailableMaps.Any(m => m.MapName == map.MapName && m.DirectoryPath == map.DirectoryPath);
        }

        public void LoadMap(string mapName)
        {
            LoadAvailableMaps();

            foreach (var map in AvailableMaps)
            {
                if (map.MapName == mapName)
                {
                    LoadSelectedMap(map);
                    return;
                }
            }
            UserMessage = $"Cannot find map with name {mapName}!";
        }

        public void ToggleVisiblityOfMap(MapListItem map)
        {
            map.IsHiddenByUser = !map.IsHiddenByUser;

            bool didWrite = MetaDataManager.WriteCustomMapPropertiesToFile(AvailableMaps);

            if (didWrite == false)
            {
                UserMessage = "Failed to update .meta file. Map may have not have been hidden.";
                return;
            }

            RefreshFilteredMaps();

            string word = map.IsHiddenByUser ? "hidden" : "visible";
            UserMessage = $"{map.DisplayName} is now {word}!";
        }

        private void SetIsSelectedForMapInList(MapListItem map)
        {
            foreach (MapListItem availableMap in AvailableMaps)
            {
                availableMap.IsSelected = false;
            }
            map.IsSelected = true;
        }

        public void LoadSelectedMap(MapListItem map)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Path To Session is not valid";
                return;
            }

            try
            {
                BoolWithMessage loadResult = MapSwitcher.LoadMap(map);

                if (loadResult.Result)
                {
                    SetIsSelectedForMapInList(map);
                    SetCurrentlyLoadedMap();
                }

                UserMessage = loadResult.Message;
            }
            catch (Exception e)
            {
                UserMessage = $"Something went wrong while loading the map: {e.Message}";
            }

        }

        public void RefreshFilteredMaps()
        {
            if (AvailableMaps != null)
            {
                NotifyPropertyChanged(nameof(FilteredAvailableMaps));
            }
        }

        public bool IsSessionUnpacked()
        {
            return UnpackUtils.IsSessionUnpacked();
        }

        public void SetCurrentlyLoadedMap()
        {
            if (MapSwitcher == null)
            {
                CurrentlyLoadedMapName = "";
                return;
            }

            string iniValue = MapSwitcher.GetGameDefaultMapSetting();

            if (iniValue.Contains("/Game/Tutorial/Intro/MAP_EntryPoint"))
            {
                CurrentlyLoadedMapName = MapSwitcher.GetDefaultSessionMap().MapName;
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

        public void OpenFolderToSession()
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

        public void OpenFolderToSessionContent()
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

        public void OpenFolderToSelectedMap(MapListItem map)
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

        private void SetRandomHintMessage()
        {
            Random random = new Random();
            int randIndex = random.Next(0, _hintMessages.Length);
            HintMessage = $"Hint: {_hintMessages[randIndex]}";
        }


        #region Methods related to EzPz Patching

        private void Patch_ProgressChanged(string message)
        {
            UserMessage = message;
        }

        public void StartPatching(bool skipPatching = false, bool skipUnpacking = false, string unrealPathFromRegistry = "")
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                UserMessage = "Cannot patch: Set Path to Session before patching game.";
                return;
            }

            _patcher = new EzPzPatcher();
            _patcher.SkipEzPzPatchStep = skipPatching;
            _patcher.SkipUnrealPakStep = skipUnpacking;
            _patcher.PathToUnrealEngine = unrealPathFromRegistry;

            _patcher.ProgressChanged += Patch_ProgressChanged;
            _patcher.PatchCompleted += EzPzPatcher_PatchCompleted;

            InputControlsEnabled = false;
            _patcher.StartPatchingAsync(SessionPath.ToSession);
        }

        private void EzPzPatcher_PatchCompleted(bool wasSuccessful)
        {
            _patcher.ProgressChanged -= Patch_ProgressChanged;
            _patcher.PatchCompleted -= EzPzPatcher_PatchCompleted;

            if (wasSuccessful)
            {
                if (_patcher.SkipEzPzPatchStep == false)
                {
                    UserMessage = "Patching complete! You should now be able to play custom maps and replace textures.";
                }
                else if (_patcher.SkipEzPzPatchStep)
                {
                    UserMessage = "Required game files extracted! You should now be able to set game settings and custom object count.";
                }

                if (MapSwitcher == null && EzPzPatcher.IsGamePatched())
                {
                    MapSwitcher = new EzPzMapSwitcher();
                }

                RefreshGameSettings();
            }
            else
            {
                UserMessage = "Patching failed. You should re-run the patching process: " + UserMessage;
            }

            _patcher = null;
            InputControlsEnabled = true;
        }

        #endregion
    }

}
