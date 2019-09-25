
using Ini.Net;
using SessionMapSwitcher.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SessionMapSwitcher.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        private string _sessionPath;
        private string _mapPath;
        private string _userMessage;
        private string _currentlyLoadedMapName;
        private ObservableCollection<MapListItem> _availableMaps;
        private MapListItem _initialLoadedMap;
        private MapListItem _defaultSessionMap;
        private bool _buttonsEnabled;

        public string SessionPath
        {
            get
            {
                if (_sessionPath.EndsWith("\\"))
                {
                    _sessionPath = _sessionPath.TrimEnd('\\');
                }
                return _sessionPath;
            }
            set
            {
                _sessionPath = value;
                NotifyPropertyChanged();
            }
        }
        public string MapPath
        {
            get
            {
                if (_mapPath.EndsWith("\\"))
                {
                    _mapPath = _mapPath.TrimEnd('\\');
                }
                return _mapPath;
            }
            set
            {
                _mapPath = value;
                NotifyPropertyChanged();
            }
        }
        public ObservableCollection<MapListItem> AvailableMaps
        {
            get
            {
                if (_availableMaps == null)
                {
                    _availableMaps = new ObservableCollection<MapListItem>();
                }
                return _availableMaps;
            }
            set
            {
                _availableMaps = value;
                NotifyPropertyChanged();
            }
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
        internal string PathToConfigFolder
        {
            get
            {
                return SessionPath + "\\SessionGame\\Config";
            }
        }

        internal string DefaultEngineConfigFilePath
        {
            get
            {
                return PathToConfigFolder + "\\DefaultEngine.ini";
            }
        }

        internal string PathToNYCFolder
        {
            get
            {
                return SessionPath + "\\SessionGame\\Content\\Art\\Env\\NYC";
            }
        }

        internal string PathToBrooklynFolder
        {
            get
            {
                return SessionPath + "\\SessionGame\\Content\\Art\\Env\\NYC\\Brooklyn";
            }
        }

        internal string PathToOriginalSessionMapFiles
        {
            get
            {
                return MapPath + "\\Original_Session_Map";
            }
        }

        internal string PathToSessionExe
        {
            get
            {
                return SessionPath + "\\SessionGame\\Binaries\\Win64\\SessionGame-Win64-Shipping.exe";
            }
        }

        public bool ButtonsEnabled
        {
            get => _buttonsEnabled;
            set
            {
                _buttonsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        internal MapListItem FirstLoadedMap { get => _initialLoadedMap; set => _initialLoadedMap = value; }

        public MainWindowViewModel()
        {
            SessionPath = AppSettingsUtil.GetAppSetting("PathToSession");
            MapPath = AppSettingsUtil.GetAppSetting("PathToMaps");
            UserMessage = "";
            ButtonsEnabled = true;

            _defaultSessionMap = new MapListItem()
            {
                FullPath = PathToOriginalSessionMapFiles,
                DisplayName = "Session Default Map - Brooklyn Banks"
            };

            LoadAvailableMaps();
            SetCurrentlyLoadedMap();
        }

        public void SetSessionPath(string pathToSession)
        {
            SessionPath = pathToSession;
            AppSettingsUtil.AddOrUpdateAppSettings("PathToSession", pathToSession);
        }

        internal bool IsSessionPathValid()
        {
            if (Directory.Exists($"{SessionPath}\\Engine") == false)
            {
                return false;
            }

            if (Directory.Exists($"{SessionPath}\\SessionGame") == false)
            {
                return false;
            }

            return true;
        }

        public void SetMapPath(string pathToMaps)
        {
            MapPath = pathToMaps;
            AppSettingsUtil.AddOrUpdateAppSettings("PathToMaps", pathToMaps);
        }

        public void LoadAvailableMaps()
        {
            if (String.IsNullOrEmpty(MapPath))
            {
                return;
            }

            if (Directory.Exists(MapPath) == false)
            {
                UserMessage = $"error: {MapPath} does not exist.";
                return;
            }

            AvailableMaps.Clear();

            // add default session map to select
            _defaultSessionMap.IsEnabled = IsOriginalMapFilesBackedUp();
            AvailableMaps.Add(_defaultSessionMap);


            // loop over files in given map directory and look for files with the '.umap' extension
            foreach (string file in Directory.GetFiles(MapPath))
            {
                if (file.EndsWith(".umap") == false)
                {
                    continue;
                }

                MapListItem mapItem = new MapListItem
                {
                    FullPath = file,
                    DisplayName = file.Replace(MapPath + "\\", "").Replace(".umap", "")
                };
                mapItem.Validate();

                if (MapIsLoaded(mapItem.DisplayName) == false)
                {
                    AvailableMaps.Add(mapItem);
                }
            }
        }

        private bool MapIsLoaded(string mapName)
        {
            return AvailableMaps.Any(m => m.DisplayName == mapName);
        }

        internal bool BackupOriginalMapFiles()
        {
            if (string.IsNullOrEmpty(MapPath))
            {
                // no path to 'Maps'
                UserMessage = "Cannot backup: 'Path To Maps' has not been set.";
                return false;
            }

            if (IsSessionPathValid() == false)
            {
                UserMessage = "Cannot backup: 'Path to Session' is invalid.";
                return false;
            }

            if (IsOriginalMapFilesBackedUp())
            {
                // the files are already backed up
                UserMessage = "Cannot backup: files already backed up.";
                return false;
            }

            if (DoesOriginalMapFilesExistInGameDirectory() == false)
            {
                // the original files are missing from the Session directory
                UserMessage = "Cannot backup: original map files for Session are missing from Session game directory.";
                return false;
            }

            try
            {
                // create folder if it doesn't exist
                Directory.CreateDirectory(PathToOriginalSessionMapFiles);

                // copy top level NYC map files
                foreach (string fullPathToFile in Directory.GetFiles(PathToNYCFolder))
                {
                    string fileName = fullPathToFile.Replace(PathToNYCFolder, "");
                    string destFilePath = PathToOriginalSessionMapFiles + fileName;

                    if (File.Exists(destFilePath) == false)
                    {
                        File.Copy(fullPathToFile, destFilePath, overwrite: true);
                    }
                }

                // copy all files in 'Brooklyn'
                FileUtils.CopyDirectoryRecursively(PathToBrooklynFolder, PathToOriginalSessionMapFiles + "\\Brooklyn", true);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to backup original map files: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            _defaultSessionMap.IsEnabled = IsOriginalMapFilesBackedUp();
            return true;
        }

        internal bool IsOriginalMapFilesBackedUp()
        {
            if (Directory.Exists(PathToOriginalSessionMapFiles) == false)
            {
                return false;
            }

            // check that a subset of the NYC files exist
            List<string> expectedFileNames = new List<string>() { "NYC01_Background.uexp", "NYC01_Persistent.uexp", "NYC01_PostProcess.uexp", "NYC01_SFX.uexp", "NYC01_Sky.uexp", "NYC01_Tutorials.uexp", "NYC01_VFX_BuiltData.uasset" };
            foreach (string fileName in expectedFileNames)
            {
                if (File.Exists($"{PathToOriginalSessionMapFiles}\\{fileName}") == false)
                {
                    return false;
                }
            }

            List<string> expectedDirNames = new List<string>() { "Background", "Decals", "Levels", "Props", "Tileables" };
            foreach (string dirName in expectedDirNames)
            {
                if (Directory.Exists($"{PathToOriginalSessionMapFiles}\\Brooklyn\\{dirName}") == false)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool DoesOriginalMapFilesExistInGameDirectory()
        {
            if (Directory.Exists(PathToNYCFolder) == false)
            {
                return false;
            }

            // check that a subset of the NYC files exist
            List<string> expectedFileNames = new List<string>() { "NYC01_Background.uexp", "NYC01_Persistent.uexp", "NYC01_PostProcess.uexp", "NYC01_SFX.uexp", "NYC01_Sky.uexp", "NYC01_Tutorials.uexp", "NYC01_VFX_BuiltData.uasset" };
            foreach (string fileName in expectedFileNames)
            {
                if (File.Exists($"{PathToNYCFolder}\\{fileName}") == false)
                {
                    return false;
                }
            }

            List<string> expectedDirNames = new List<string>() { "Background", "Decals", "Levels", "Props", "Tileables" };
            foreach (string dirName in expectedDirNames)
            {
                if (Directory.Exists($"{PathToNYCFolder}\\Brooklyn\\{dirName}") == false)
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

        internal void CopyMapFilesToGame()
        {

        }

        internal void LoadMap(MapListItem map)
        {
            if (IsSessionPathValid() == false)
            {
                UserMessage = "Cannot Load: 'Path to Session' is invalid.";
                return;
            }

            if (map == _defaultSessionMap)
            {
                LoadOriginalMap();
                return;
            }

            try
            {
                if (IsSessionRunning() == false || FirstLoadedMap == null)
                {
                    FirstLoadedMap = map;
                }

                // delete original session map files + custom maps from game
                DeleteAllMapFilesFromGame();

                string pathToLevelsFolder = PathToBrooklynFolder + "\\Levels";

                Directory.CreateDirectory(pathToLevelsFolder);
                System.Threading.Thread.Sleep(1000); // sleep for 1 second to avoid race condition where the newly created Directory cannot be copied to

                if (Directory.Exists(pathToLevelsFolder) == false)
                {
                    // check again due to race condition that the OS has created the directory
                    // ... wait a second then try creating again.
                    System.Threading.Thread.Sleep(1000);
                    Directory.CreateDirectory(pathToLevelsFolder);
                }

                // copy all files related to map to game directory
                foreach (string fileName in Directory.GetFiles(MapPath))
                {
                    if (fileName.Contains(map.DisplayName))
                    {
                        FileInfo fi = new FileInfo(fileName);
                        string fullTargetFilePath = pathToLevelsFolder;


                        if (IsSessionRunning() && FirstLoadedMap != null)
                        {
                            // while the game is running, the map being loaded must have the same name as the initial map that was loaded when the game first started.
                            // ... thus we build the destination filename based on what was first loaded.
                            if (fileName.Contains("_BuiltData"))
                            {
                                fullTargetFilePath += $"\\{FirstLoadedMap.DisplayName}_BuiltData{fi.Extension}";
                            }
                            else
                            {
                                fullTargetFilePath += $"\\{FirstLoadedMap.DisplayName}{fi.Extension}";
                            }
                        }
                        else
                        {
                            // if the game is not running then the files can be copied as-is (file names do not need to be changed)
                            string targetFileName = fileName.Replace(MapPath, "");
                            fullTargetFilePath += targetFileName;
                        }

                        File.Copy(fileName, fullTargetFilePath, true);
                    }
                }

                // update the ini file with the new map path
                string selectedMapPath = "/Game/Art/Env/NYC/Brooklyn/Levels/" + map.DisplayName;
                UpdateGameDefaultMapIniSetting(selectedMapPath);

                SetCurrentlyLoadedMap();

                UserMessage = $"{map.DisplayName} Loaded!";
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load {map.DisplayName}: {e.Message}";
            }
        }

        private bool UpdateGameDefaultMapIniSetting(string defaultMapValue)
        {
            if (IsSessionPathValid() == false)
            {
                return false;
            }

            IniFile iniFile = new IniFile(DefaultEngineConfigFilePath);
            return iniFile.WriteString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap", defaultMapValue);
        }

        private string GetGameDefaultMapIniSetting()
        {
            if (IsSessionPathValid() == false)
            {
                return "";
            }

            IniFile iniFile = new IniFile(DefaultEngineConfigFilePath);
            return iniFile.ReadString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap");
        }

        internal void LoadOriginalMap()
        {
            try
            {
                DeleteAllMapFilesFromGame();

                foreach (string fileName in Directory.GetFiles(PathToOriginalSessionMapFiles))
                {
                    string targetFileName = fileName.Replace(PathToOriginalSessionMapFiles, "");
                    string fullTargetFilePath = PathToNYCFolder + targetFileName;

                    if (File.Exists(fullTargetFilePath) == false)
                    {
                        File.Copy(fileName, fullTargetFilePath, true);
                    }
                }

                FileUtils.CopyDirectoryRecursively(PathToOriginalSessionMapFiles + "\\Brooklyn", PathToBrooklynFolder, true);

                UpdateGameDefaultMapIniSetting("/Game/Tutorial/Intro/MAP_EntryPoint.MAP_EntryPoint");

                SetCurrentlyLoadedMap();

                UserMessage = $"{_defaultSessionMap.DisplayName} Loaded!";

            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load Original Session Game Map : {e.Message}";
            }
        }

        private void DeleteAllMapFilesFromGame()
        {
            if (IsSessionPathValid() == false)
            {
                return;
            }

            foreach (string fileName in Directory.GetFiles(PathToNYCFolder))
            {
                File.Delete(fileName);
            }

            foreach (string directoryName in Directory.GetDirectories(PathToBrooklynFolder))
            {
                Directory.Delete(directoryName, true);
            }
        }

        internal void SetCurrentlyLoadedMap()
        {
            string iniValue = GetGameDefaultMapIniSetting();

            if (iniValue == "/Game/Tutorial/Intro/MAP_EntryPoint.MAP_EntryPoint")
            {
                CurrentlyLoadedMapName = _defaultSessionMap.DisplayName;
            }
            else if (String.IsNullOrEmpty(iniValue) == false)
            {
                int startIndex = iniValue.LastIndexOf("/") + 1;
                CurrentlyLoadedMapName = iniValue.Substring(startIndex, iniValue.Length - startIndex);
            }
        }
    }

}
