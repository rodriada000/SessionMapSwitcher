
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
            SessionPathTextInput = SessionPath.ToSession;
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

            RefreshGameSettings();
            SetCurrentlyLoadedMap();
        }

        internal void RefreshGameSettings()
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

        internal bool UpdateGameSettings(bool promptToDownloadIfMissing)
        {

            if (GameSettingsManager.DoesObjectPlacementFileExist() == false)
            {
                if (promptToDownloadIfMissing)
                {
                    MessageBoxResult promptResult = MessageBox.Show("The required files are missing and must be extracted before game settings can be modified.\n\nClick 'Yes' to extract the files (UnrealPak and crypto.json will be downloaded if it is not installed locally).",
                                                                    "Warning - Cannot Continue!",
                                                                    MessageBoxButton.YesNo,
                                                                    MessageBoxImage.Information,
                                                                    MessageBoxResult.Yes);

                    if (promptResult == MessageBoxResult.Yes)
                    {
                        StartPatching(skipPatching: true, skipUnpacking: false);
                        return false;
                    }
                }

                UserMessage = "Custom gravity and object count will not be applied until required files are extracted.";
                return false;
            }

            InputControlsEnabled = false;

            BoolWithMessage result = GameSettingsManager.WriteGameSettingsToFile(GravityText, ObjectCountText, SkipMovieIsChecked);

            if (result.Result)
            {
                RefreshGameSettings();
            }
            else
            {
                UserMessage = result.Message;
            }

            InputControlsEnabled = true;

            return result.Result;
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
                _defaultSessionMap.FullPath = SessionPath.ToOriginalSessionMapFiles;
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


                    if (IsSessionRunning())
                    {
                        // While Session is running the map files must be copied as NYC01_Persistent so when the user leaves the apartment the custom map is loaded
                        fullTargetFilePath += $"\\NYC01_Persistent";

                        if (fileName.Contains("_BuiltData"))
                        {
                            fullTargetFilePath += $"_BuiltData{fi.Extension}";
                        }
                        else
                        {
                            fullTargetFilePath += fi.Extension;
                        }
                    }
                    else
                    {
                        fullTargetFilePath += $"\\{fi.Name}";
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

            SetIsSelectedForMapInList(map);

            if (Directory.Exists(SessionPath.ToNYCFolder) == false)
            {
                Directory.CreateDirectory(SessionPath.ToNYCFolder);
            }

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

                if (IsSessionRunning() == false)
                {
                    selectedMapPath = $"/Game/Art/Env/NYC/{map.MapName}";
                }

                GameSettingsManager.UpdateGameDefaultMapIniSetting(selectedMapPath);

                SetCurrentlyLoadedMap();

                UserMessage = $"{map.MapName} Loaded!";
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load {map.MapName}: {e.Message}";
            }
        }

        private void SetIsSelectedForMapInList(MapListItem map)
        {
            foreach (MapListItem availableMap in AvailableMaps)
            {
                availableMap.IsSelected = false;
            }
            map.IsSelected = true;
        }

        internal void LoadOriginalMap()
        {
            try
            {
                DeleteAllMapFilesFromNYCFolder();

                GameSettingsManager.UpdateGameDefaultMapIniSetting("/Game/Tutorial/Intro/MAP_EntryPoint");

                SetCurrentlyLoadedMap();

                UserMessage = $"{_defaultSessionMap.MapName} Loaded!";

            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load Original Session Game Map : {e.Message}";
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
            string iniValue = GameSettingsManager.GetGameDefaultMapIniSetting();

            if (iniValue == "/Game/Tutorial/Intro/MAP_EntryPoint")
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


        #region Methods related to EzPz Patching

        private void Patch_ProgressChanged(string message)
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
                StartPatching(skipPatching: false, skipUnpacking: true);
            }
        }

        internal void StartPatching(bool skipPatching = false, bool skipUnpacking = false)
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
            _patcher.SkipEzPzPatchStep = skipPatching;
            _patcher.SkipUnrealPakStep = skipUnpacking;

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
