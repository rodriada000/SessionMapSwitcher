
using Ini.Net;
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
        private string _mapPath;
        private string _userMessage;
        private string _currentlyLoadedMapName;
        private ObservableCollection<MapListItem> _availableMaps;
        private MapListItem _firstLoadedMap;
        private MapListItem _defaultSessionMap;
        private bool _inputControlsEnabled;
        private bool _showInvalidMaps;
        private string _gravityText;
        private string _objectCountText;
        private bool _skipMovieIsChecked;
        private const string _backupFolderName = "Original_Session_Map";

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
            return ShowInvalidMapsIsChecked || (!ShowInvalidMapsIsChecked && p.IsValid);
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
                return $"{SessionPath}\\SessionGame\\Config";
            }
        }
        internal string DefaultEngineIniFilePath
        {
            get
            {
                return $"{PathToConfigFolder}\\DefaultEngine.ini";
            }
        }

        internal string DefaultGameIniFilePath
        {
            get
            {
                return $"{PathToConfigFolder}\\DefaultGame.ini";
            }
        }

        /// <summary>
        /// Returns absolute path to the NYC folder in Session game directory. Requires <see cref="SessionPath"/>.
        /// </summary>
        internal string PathToNYCFolder
        {
            get
            {
                return $"{SessionPath}\\SessionGame\\Content\\Art\\Env\\NYC";
            }
        }
        internal string PathToBrooklynFolder
        {
            get
            {
                return $"{PathToNYCFolder}\\Brooklyn";
            }
        }
        internal string PathToOriginalSessionMapFiles
        {
            get
            {
                return $"{MapPath}\\{_backupFolderName}";
            }
        }
        internal string PathToSessionExe
        {
            get
            {
                return $"{SessionPath}\\SessionGame\\Binaries\\Win64\\SessionGame-Win64-Shipping.exe";
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
                    NotifyPropertyChanged();
                    AppSettingsUtil.AddOrUpdateAppSettings("ShowInvalidMaps", value.ToString());
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

        #endregion


        public MainWindowViewModel()
        {
            SessionPath = AppSettingsUtil.GetAppSetting("PathToSession");
            MapPath = AppSettingsUtil.GetAppSetting("PathToMaps");
            ShowInvalidMapsIsChecked = AppSettingsUtil.GetAppSetting("ShowInvalidMaps").Equals("true", StringComparison.OrdinalIgnoreCase);
            UserMessage = "";
            InputControlsEnabled = true;

            _defaultSessionMap = new MapListItem()
            {
                FullPath = PathToOriginalSessionMapFiles,
                DisplayName = "Session Default Map - Brooklyn Banks"
            };

            RefreshGameSettingsFromIniFiles();

            SetCurrentlyLoadedMap();

            GetObjectCountFromFile();
        }

        /// <summary>
        /// Sets <see cref="SessionPath"/> and saves the value to appSettings in the applications .config file
        /// </summary>
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

        public bool LoadAvailableMaps()
        {
            AvailableMaps.Clear();

            if (String.IsNullOrEmpty(MapPath))
            {
                UserMessage = $"Cannot load available maps: 'Path To Maps' is missing.";
                return false;
            }

            if (Directory.Exists(MapPath) == false)
            {
                UserMessage = $"Cannot load available maps: {MapPath} does not exist.";
                return false;
            }

            try
            {
                LoadAvailableMapsInSubDirectories(MapPath);
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load available maps: {e.Message}";
                return false;
            }

            AvailableMaps = new ObservableCollection<MapListItem>(AvailableMaps.OrderBy(m => m.DisplayName));

            // add default session map to select (add last so it is always at top of list)
            _defaultSessionMap.IsEnabled = IsOriginalMapFilesBackedUp();
            _defaultSessionMap.FullPath = PathToOriginalSessionMapFiles;
            _defaultSessionMap.Tooltip = _defaultSessionMap.IsEnabled ? null : "The original Session game files have not been backed up to the custom Maps folder.";
            AvailableMaps.Insert(0, _defaultSessionMap);

            UserMessage = "List of available maps loaded!";
            return true;
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
                    DisplayName = file.Replace(dirToSearch + "\\", "").Replace(".umap", "")
                };
                mapItem.Validate();

                if (mapItem.DirectoryPath.Contains(PathToNYCFolder))
                {
                    // skip files that are known to be apart of the original map so they are not displayed in list of avaiable maps (like the NYC01_Persistent.umap file)
                    continue;
                }

                if (IsMapAdded(mapItem.DisplayName) == false)
                {
                    AvailableMaps.Add(mapItem);
                }
            }

            // recursively search for .umap files in sub directories (skipping 'Original_Session_Map')
            foreach (string subFolder in Directory.GetDirectories(dirToSearch))
            {
                DirectoryInfo info = new DirectoryInfo(subFolder);

                if (info.Name != _backupFolderName)
                {
                    LoadAvailableMapsInSubDirectories(subFolder);
                }
            }
        }

        private bool IsMapAdded(string mapName)
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
                UserMessage = "Skipping backup: original files already backed up.";
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

                // copy all files in 'Brooklyn\Levels'
                FileUtils.CopyDirectoryRecursively($"{PathToBrooklynFolder}\\Levels", $"{PathToOriginalSessionMapFiles}\\Brooklyn\\Levels", true);
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

            if (Directory.Exists($"{PathToOriginalSessionMapFiles}\\Brooklyn\\Levels") == false)
            {
                return false;
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

        private bool CopyMapFilesToGame(MapListItem map)
        {
            if (IsSessionPathValid() == false)
            {
                return false;
            }

            string pathToLevelsFolder = PathToBrooklynFolder + "\\Levels";

            Directory.CreateDirectory(pathToLevelsFolder);
            System.Threading.Thread.Sleep(500); // sleep for 1 second to avoid race condition where the newly created Directory cannot be copied to

            if (Directory.Exists(pathToLevelsFolder) == false)
            {
                // check again due to race condition that the OS has created the directory
                // ... wait a second then try creating again.
                System.Threading.Thread.Sleep(500);
                Directory.CreateDirectory(pathToLevelsFolder);
            }

            // copy all files related to map to game directory
            foreach (string fileName in Directory.GetFiles(map.DirectoryPath))
            {
                if (fileName.Contains(map.DisplayName))
                {
                    FileInfo fi = new FileInfo(fileName);
                    string fullTargetFilePath = PathToNYCFolder;


                    if (IsSessionRunning() && FirstLoadedMap != null)
                    {
                        // while the game is running, the map being loaded must have the same name as the initial map that was loaded when the game first started.
                        // ... thus we build the destination filename based on what was first loaded.
                        if (FirstLoadedMap == _defaultSessionMap)
                        {
                            fullTargetFilePath += $"\\NYC01_Persistent"; // this is the name of the default map that is loaded
                        }
                        else
                        {
                            fullTargetFilePath += $"\\{FirstLoadedMap.DisplayName}";
                        }

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
                        // if the game is not running then the files can be copied as-is (file names do not need to be changed)
                        string targetFileName = fileName.Replace(map.DirectoryPath, "");
                        fullTargetFilePath += targetFileName;
                    }

                    File.Copy(fileName, fullTargetFilePath, true);
                }
            }

            return true;
        }

        internal void LoadMap(MapListItem map)
        {
            if (IsSessionPathValid() == false)
            {
                UserMessage = "Cannot Load: 'Path to Session' is invalid.";
                return;
            }

            if (IsSessionRunning() == false || FirstLoadedMap == null)
            {
                FirstLoadedMap = map;
            }

            if (map == _defaultSessionMap)
            {
                LoadOriginalMap();
                return;
            }

            try
            {
                // delete original session map files + custom maps from game before loading new map
                DeleteAllMapFilesFromGame();

                CopyMapFilesToGame(map);

                // update the ini file with the new map path
                string selectedMapPath = "/Game/Art/Env/NYC/" + map.DisplayName;
                UpdateGameDefaultMapIniSetting(selectedMapPath);

                SetCurrentlyLoadedMap();

                UserMessage = $"{map.DisplayName} Loaded!";
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to load {map.DisplayName}: {e.Message}";
            }
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

        private bool UpdateGameDefaultMapIniSetting(string defaultMapValue)
        {
            if (IsSessionPathValid() == false)
            {
                return false;
            }

            IniFile iniFile = new IniFile(DefaultEngineIniFilePath);
            return iniFile.WriteString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap", defaultMapValue);
        }

        private string GetGameDefaultMapIniSetting()
        {
            if (IsSessionPathValid() == false)
            {
                return "";
            }

            IniFile iniFile = new IniFile(DefaultEngineIniFilePath);
            return iniFile.ReadString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap");
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

            if (Directory.Exists($"{PathToBrooklynFolder}\\Levels"))
            {
                Directory.Delete($"{PathToBrooklynFolder}\\Levels", true);
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

        internal void OpenFolderToSession()
        {
            try
            {
                Process.Start(SessionPath);
            }
            catch (Exception ex)
            {
                UserMessage = $"Cannot open folder: {ex.Message}";
            }
        }

        internal void OpenFolderToAvailableMaps()
        {
            try
            {
                Process.Start(MapPath);
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
            if (IsSessionPathValid() == false)
            {
                return;
            }

            IniFile engineFile = new IniFile(DefaultEngineIniFilePath);
            GravityText = engineFile.ReadString("/Script/Engine.PhysicsSettings", "DefaultGravityZ");

            IniFile gameFile = new IniFile(DefaultGameIniFilePath);
            SkipMovieIsChecked = gameFile.ReadBoolean("/Script/UnrealEd.ProjectPackagingSettings", "bSkipMovies");
        }

        /// <summary>
        /// writes the game settings to the correct files.
        /// </summary>
        /// <returns> true if settings updated; false otherwise. </returns>
        public bool WriteGameSettingsToFile()
        {
            if (IsSessionPathValid() == false)
            {
                return false;
            }

            if (float.TryParse(GravityText, out float parsedFloat) == false)
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

            IniFile engineFile = new IniFile(DefaultEngineIniFilePath);
            engineFile.WriteString("/Script/Engine.PhysicsSettings", "DefaultGravityZ", GravityText);

            IniFile gameFile = new IniFile(DefaultGameIniFilePath);

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

            return true;
        }


        /// <summary>
        /// Get the Object Placement count from the file (only reads the first address) and set <see cref="ObjectCountText"/>
        /// </summary>
        internal void GetObjectCountFromFile()
        {
            string objectFilePath = $"{SessionPath}\\SessionGame\\Content\\ObjectPlacement\\Blueprints\\PBP_ObjectPlacementInventory.uexp";
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

        /// <summary>
        /// Updates the PBP_ObjectPlacementInventory.uexp file with the new object count value (every placeable object is updated with new count).
        /// This works by converting <see cref="ObjectCountText"/> to bytes and writing the bytes to specific addresses in the file.
        /// </summary>
        internal void SetObjectCountInFile()
        {
            string objectFilePath = $"{SessionPath}\\SessionGame\\Content\\ObjectPlacement\\Blueprints\\PBP_ObjectPlacementInventory.uexp";

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
    }

}
