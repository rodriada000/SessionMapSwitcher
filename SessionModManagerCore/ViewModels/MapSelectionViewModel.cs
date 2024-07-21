using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Classes.Interfaces;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace SessionModManagerCore.ViewModels
{
    public class MapSelectionViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        #region Data Members And Properties

        private string _sessionPath;
        private string _hintMessage;
        private string _currentlyLoadedMapName;
        private List<MapListItem> _availableMaps;
        private object collectionLock = new object();
        private bool _inputControlsEnabled;
        private bool _showInvalidMaps;
        private MapListItem _secondMapToLoad;
        private bool _loadSecondMapIsChecked;
        private bool _rmsToolsuiteIsChecked;

        private Stream _mapPreviewSource;
        private bool _isLoadingImage;

        private IMapSwitcher MapSwitcher { get; set; }

        private readonly string[] _hintMessages = new string[] { "Right-click list of maps and click 'Open Content Folder ...' to get to the Content folder easily",
                                                                 "Download maps and mods from the Asset Store tab",
                                                                 "Hide maps in the list by right-clicking the map and clicking 'Hide Selected Map ...'",
                                                                 "Rename maps in the list by right-clicking the map and clicking 'Rename Selected Map ...'",
                                                                 "Use Project Watcher to auto-import your map after you cooked it in Unreal Engine",
                                                                 "Creators: zip up your mods with a preview.png file to include a preview image for the map/mod",
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

        public string SecondMapCheckboxText
        {
            get
            {
                if (SecondMapToLoad == null && LoadSecondMapIsChecked == false)
                {
                    return $"Load Second Map After Start (Not Set)";
                }
                else if (SecondMapToLoad == null && LoadSecondMapIsChecked)
                {
                    // the currently loaded map will be used if second map to load is null but the option is checked
                    return $"Load Second Map After Start ({CurrentlyLoadedMapName})";
                }

                return $"Load Second Map After Start ({SecondMapToLoad.DisplayName})";
            }
        }

        public bool LoadSecondMapIsChecked
        {
            get { return _loadSecondMapIsChecked; }
            set
            {
                _loadSecondMapIsChecked = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(SecondMapCheckboxText));
            }
        }

        public bool RMSToolsuiteIsChecked
        {
            get
            {
                return _rmsToolsuiteIsChecked;
            }
            set
            {
                if (value != _rmsToolsuiteIsChecked)
                {
                    _rmsToolsuiteIsChecked = value;
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.EnableRMSTools, value.ToString());
                    NotifyPropertyChanged();
                }
            }
        }

        public bool RMSToolsuiteCheckboxIsVisible
        {
            get
            {
                return RMSToolsuiteLoader.IsToolsuiteInstalled();
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
                NotifyPropertyChanged(nameof(SecondMapCheckboxText));
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

        public string LoadMapButtonText
        {
            get
            {
                return "Load Map";
            }
        }

        /// <summary>
        /// Map to load after starting the game in <see cref="StartGameAndLoadSecondMap"/>
        /// If null then the currently loaded map will be used.
        /// </summary>
        public MapListItem SecondMapToLoad
        {
            get => _secondMapToLoad;
            set
            {
                _secondMapToLoad = value;
                NotifyPropertyChanged(nameof(SecondMapCheckboxText));
            }
        }

        public Stream MapPreviewSource
        {
            get { return _mapPreviewSource; }
            set
            {
                _mapPreviewSource = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsPreviewMissing));
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
                NotifyPropertyChanged(nameof(IsPreviewMissing));
            }
        }

        public bool IsPreviewMissing
        {
            get
            {
                return (MapPreviewSource == null && !IsLoadingImage);
            }
        }
        #endregion


        public MapSelectionViewModel()
        {
            SessionPathTextInput = SessionPath.ToSession;
            ShowInvalidMapsIsChecked = AppSettingsUtil.GetAppSetting(SettingKey.ShowInvalidMaps).Equals("true", StringComparison.OrdinalIgnoreCase);

            string savedSetting = AppSettingsUtil.GetAppSetting(SettingKey.EnableRMSTools);
            if (savedSetting == "")
            {
                RMSToolsuiteIsChecked = true; // default to true if no setting saved
            }
            else
            {
                RMSToolsuiteIsChecked = savedSetting.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            InputControlsEnabled = true;

            SetRandomHintMessage();
            InitMapSwitcher();
        }

        private void InitMapSwitcher()
        {
            if (UeModUnlocker.IsGamePatched())
            {
                MapSwitcher = new EzPzMapSwitcher();
            }

            if (MapSwitcher != null)
            {
                SetCurrentlyLoadedMap();
            }
        }

        public void StartGameAndLoadSecondMap()
        {
            // check if RMS toolsuite is installed before starting game and then enable/disable it
            if (RMSToolsuiteLoader.IsToolsuiteInstalled())
            {
                if (RMSToolsuiteIsChecked)
                {
                    RMSToolsuiteLoader.CopyFilesToEnvFolder();
                }
                else
                {
                    RMSToolsuiteLoader.DeleteFilesInEnvFolder();

                }
            }

            if (AppSettingsUtil.GetAppSetting(SettingKey.LaunchViaSteam)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    Process.Start(new ProcessStartInfo("steam://launch/861650/dialog") { UseShellExecute = true });
                }
                catch (Exception)
                {
                    // if fails to launch via steam then just launch the .exe directly
                    Logger.Warn("Failed to launch via steam://launch/861650/dialog command. Defaulting to .exe ...");
                    Process.Start(SessionPath.ToSessionExe);
                }
                
            }
            else
            {
                Process.Start(SessionPath.ToSessionExe);
            }


            if (LoadSecondMapIsChecked == false)
            {
                return; // do not continue as the second map does not need to be loaded
            }


            Task loadTask = Task.Factory.StartNew(() =>
            {
                MapListItem mapToLoadNext = SecondMapToLoad;

                if (mapToLoadNext == null)
                {
                    mapToLoadNext = AvailableMaps.Where(m => m.MapName == CurrentlyLoadedMapName).FirstOrDefault();
                }

                int timeToWaitInMilliseconds = 20000;

                if (MapSwitcher is UnpackedMapSwitcher)
                {
                    // wait longer for unpacked games to load since they load slower
                    timeToWaitInMilliseconds = 25000;
                }

                System.Threading.Thread.Sleep(timeToWaitInMilliseconds); // wait few seconds before loading the next map to let the game finish loading
                LoadSelectedMap(mapToLoadNext);
            });

            loadTask.ContinueWith((result) =>
            {
                if (result.IsFaulted)
                {
                    Logger.Warn(result.Exception.GetBaseException(), "failed to load second map");
                    return;
                }
            });
        }

        public void CheckForRMSTools()
        {
            NotifyPropertyChanged(nameof(RMSToolsuiteCheckboxIsVisible));
        }

        /// <summary>
        /// Sets <see cref="SessionPath.ToSession"/> and saves the value to appSettings in the applications .config file
        /// </summary>
        public void SetSessionPath(string pathToSession)
        {
            SessionPath.ToSession = pathToSession;
            SessionPathTextInput = pathToSession;
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.PathToSession, SessionPath.ToSession);

            if (UeModUnlocker.IsGamePatched())
            {
                MapSwitcher = new EzPzMapSwitcher();
            }
            else
            {
                MapSwitcher = null;
            }
        }

        public void ReloadAvailableMapsInBackground(bool showLoadingMessage = true)
        {
            if (showLoadingMessage)
            {
                MessageService.Instance.ShowMessage($"Reloading Available Maps ...");
            }

            InputControlsEnabled = false;

            Task t = Task.Factory.StartNew(() => LoadAvailableMaps());

            t.ContinueWith((antecedent) =>
            {
                InputControlsEnabled = true;
            });
        }

        public bool LoadAvailableMaps()
        {
            lock (collectionLock)
            {
                AvailableMaps.Clear();
            }

            if (SessionPath.IsSessionPathValid() == false)
            {
                MessageService.Instance.ShowMessage($"Cannot load available maps: 'Path To Session' has not been set.");
                return false;
            }

            if (Directory.Exists(SessionPath.ToContent) == false)
            {
                MessageService.Instance.ShowMessage($"Cannot load available maps: {SessionPath.ToContent} does not exist. Make sure the Session Path is set correctly.");
                return false;
            }

            InitMapSwitcher();

            try
            {
                LoadAvailableMapsInSubDirectories(SessionPath.ToContent);
                MetaDataManager.SetCustomPropertiesForMaps(AvailableMaps, createIfNotExists: true);
            }
            catch (Exception e)
            {
                MessageService.Instance.ShowMessage($"Failed to load available maps: {e.Message}");
                return false;
            }

            lock (collectionLock)
            {

                // sort the maps A -> Z
                AvailableMaps = new List<MapListItem>(AvailableMaps.OrderBy(m => m.DisplayName));

                // add default maps after so they are not sorted and always at top of list
                AddDefaultMapToAvailableMaps();

                SelectCurrentlyLoadedMapInList();
            }

            MetaDataManager.SetCustomPropertiesForMaps(AvailableMaps, createIfNotExists: true);
            NotifyPropertyChanged(nameof(FilteredAvailableMaps));

            MessageService.Instance.ShowMessage("List of available maps loaded!");
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

            var defaultMaps = MapSwitcher.GetDefaultSessionMaps();
            for (int i = defaultMaps.Count - 1; i >= 0; i--)
            {
                AvailableMaps.Insert(0, defaultMaps[i]);
            }

            NotifyPropertyChanged(nameof(AvailableMaps));
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
                MessageService.Instance.ShowMessage("Failed to re-import: Path To Session is invalid.");
                return;
            }

            MapMetaData existingMetaData = MetaDataManager.LoadMapMetaData(selectedItem);

            MapImportViewModel importViewModel = new MapImportViewModel()
            {
                IsZipFileImport = false,
                PathInput = MetaDataManager.GetOriginalImportLocation(selectedItem)
            };

            MessageService.Instance.ShowMessage("Re-importing in progress ...");

            importViewModel.ImportMapAsync().ContinueWith(
                (antecedent) =>
                {
                    if (antecedent.Result.Result)
                    {
                        MessageService.Instance.ShowMessage("Map Re-imported Successfully! Reloading maps ...");
                        MetaDataManager.SaveMapMetaData(existingMetaData); // re-save meta data to keep original import path and other custom properties
                        ReloadAvailableMapsInBackground(showLoadingMessage: false);
                    }
                    else
                    {
                        MessageService.Instance.ShowMessage($"Failed to re-import map: {antecedent.Result.Message}");
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
            MessageService.Instance.ShowMessage($"Cannot find map with name {mapName}!");
        }

        public void ToggleVisiblityOfMap(MapListItem map)
        {
            map.IsHiddenByUser = !map.IsHiddenByUser;

            bool didWrite = MetaDataManager.WriteCustomMapPropertiesToFile(map);

            if (didWrite == false)
            {
                MessageService.Instance.ShowMessage("Failed to update .meta file. Map may have not have been hidden.");
                return;
            }

            RefreshFilteredMaps();

            string word = map.IsHiddenByUser ? "hidden" : "visible";
            MessageService.Instance.ShowMessage($"{map.DisplayName} is now {word}!");
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
                MessageService.Instance.ShowMessage("Path To Session is not valid");
                return;
            }

            try
            {
                BoolWithMessage loadResult = MapSwitcher.LoadMap(map);

                if (loadResult.Result)
                {
                    SetIsSelectedForMapInList(map);
                    CurrentlyLoadedMapName = map.MapName;
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.LastSelectedMap, map.MapName);
                }

                MessageService.Instance.ShowMessage(loadResult.Message);
            }
            catch (Exception e)
            {
                MessageService.Instance.ShowMessage($"Something went wrong while loading the map: {e.Message}");
            }

        }

        public void RefreshFilteredMaps()
        {
            if (AvailableMaps != null)
            {
                NotifyPropertyChanged(nameof(FilteredAvailableMaps));
            }
        }

        public void SetCurrentlyLoadedMap()
        {
            if (MapSwitcher == null)
            {
                CurrentlyLoadedMapName = "";
                return;
            }

            CurrentlyLoadedMapName = AppSettingsUtil.GetAppSetting(SettingKey.LastSelectedMap);
        }

        public void OpenFolderToSession()
        {
            try
            {
                Process.Start(new ProcessStartInfo(SessionPath.ToSession) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageService.Instance.ShowMessage($"Cannot open folder: {ex.Message}");
            }
        }

        public void OpenFolderToSaveFiles()
        {
            try
            {
                Process.Start(new ProcessStartInfo(SessionPath.ToSaveGamesFolder) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageService.Instance.ShowMessage($"Cannot open folder: {ex.Message}");
            }
        }

        public void OpenFolderToSessionContent()
        {
            try
            {
                Process.Start(new ProcessStartInfo(SessionPath.ToContent) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageService.Instance.ShowMessage($"Cannot open folder: {ex.Message}");
            }
        }

        public void OpenFolderToSelectedMap(MapListItem map)
        {
            try
            {
                Process.Start(new ProcessStartInfo(map.DirectoryPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageService.Instance.ShowMessage($"Cannot open folder: {ex.Message}");
            }
        }

        private void SetRandomHintMessage()
        {
            Random random = new Random();
            int randIndex = random.Next(0, _hintMessages.Length);
            HintMessage = $"Hint: {_hintMessages[randIndex]}";
        }

        public void DeleteSelectedMap(MapListItem mapToDelete)
        {
            MapMetaData metaData = MetaDataManager.LoadMapMetaData(mapToDelete);

            BoolWithMessage deleteResult = MetaDataManager.DeleteMapFiles(metaData);

            if (deleteResult.Result)
            {
                MessageService.Instance.ShowMessage($"{deleteResult.Message} ... Reloading maps ...");
                ReloadAvailableMapsInBackground(showLoadingMessage: false);
            }
        }

        public void SetOrClearSecondMapToLoad(MapListItem selectedItem)
        {
            if (SecondMapToLoad == null || SecondMapToLoad?.FullPath != selectedItem.FullPath)
            {
                SecondMapToLoad = selectedItem;
                MessageService.Instance.ShowMessage($"{selectedItem.DisplayName} will be the next map to load when you leave the apartment after starting the game!");
            }
            else
            {
                SecondMapToLoad = null;
                MessageService.Instance.ShowMessage($"Cleared! The next map to load when you leave the apartment will be the same map you load before starting the game.");
            }
        }

        public void GetSelectedPreviewImageAsync(MapListItem selectedItem)
        {
            if (selectedItem == null || string.IsNullOrWhiteSpace(selectedItem.PathToImage))
            {
                MapPreviewSource = null;
                return;
            }

            IsLoadingImage = true;

            Task t = Task.Factory.StartNew(() =>
            {
                MapPreviewSource = new MemoryStream(File.ReadAllBytes(selectedItem.PathToImage));
            });

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    MessageService.Instance.ShowMessage("Failed to get preview image.");
                    MapPreviewSource = null;
                    Logger.Error(taskResult.Exception);
                }

                IsLoadingImage = false;
            });

        }
    }

}
