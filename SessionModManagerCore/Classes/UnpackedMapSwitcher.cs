using IniParser;
using IniParser.Model;
using SessionMapSwitcherCore.Classes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace SessionMapSwitcherCore.Classes
{
    /// <summary>
    /// Class to handle loading maps for unpacked patched games.
    /// Also has helper methods for backing up original map files (for unpacked versions)
    /// </summary>
    public class UnpackedMapSwitcher : IMapSwitcher
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public List<MapListItem> DefaultMaps { get; }
        internal MapListItem FirstLoadedMap { get; set; }

        public UnpackedMapSwitcher()
        {
            DefaultMaps = new List<MapListItem>()
            {
                new MapListItem()
                {
                    GameDefaultMapSetting ="/Game/Tutorial/Intro/MAP_EntryPoint",
                    MapName = "Session Default Map - Brooklyn Banks",
                    IsDefaultMap = true
                },
                new MapListItem()
                {
                    GameDefaultMapSetting = "/Game/Art/Env/GYM/crea-turePark/GYM_crea-turePark_Persistent.GYM_crea-turePark_Persistent",
                    GlobalDefaultGameModeSetting = "/Game/Data/PBP_InGameSessionGameMode.PBP_InGameSessionGameMode_C",
                    MapName = "Crea-ture Dev Park",
                    IsDefaultMap = true
                },
                new MapListItem()
                {
                    GameDefaultMapSetting ="/Game/TEMP/GYM/FilmerMode_Gym",
                    MapName = "FilmerMode Gym Dev Park",
                    IsDefaultMap = true
                },
                new MapListItem()
                {
                    GameDefaultMapSetting = "/Game/Art/Env/GYM/DevGyms/GYM_Dev_Grindabru",
                    MapName = "Grindabru Dev Park",
                    IsDefaultMap = true
                },
                new MapListItem()
                {
                    GameDefaultMapSetting ="/Game/TEMP/chris/GrindCity_Yeah",
                    MapName = "Grind City Yeah Dev Park",
                    IsDefaultMap = true
                },
                new MapListItem()
                {
                    GameDefaultMapSetting ="/Game/TEMP/mah/TrickMap",
                    MapName = "Mah TrickMap Dev Park",
                    IsDefaultMap = true
                },
                new MapListItem()
                {
                    GameDefaultMapSetting ="/Game/TEMP/Vince/VinceMap",
                    MapName = "Vinces Dev Map",
                    IsDefaultMap = true
                }
            };
        }


        public List<MapListItem> GetDefaultSessionMaps()
        {
            return DefaultMaps;
        }

        public MapListItem GetFirstLoadedMap()
        {
            return FirstLoadedMap;
        }

        public bool CopyMapFilesToNYCFolder(MapListItem map)
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


                    if (SessionPath.IsSessionRunning() && FirstLoadedMap != null)
                    {
                        // while the game is running, the map being loaded must have the same name as the initial map that was loaded when the game first started.
                        // ... thus we build the destination filename based on what was first loaded.
                        if (FirstLoadedMap.IsDefaultMap)
                        {
                            fullTargetFilePath = Path.Combine(fullTargetFilePath, "NYC01_Persistent"); // this is the name of the default map that is loaded
                        }
                        else
                        {
                            fullTargetFilePath = Path.Combine(fullTargetFilePath, FirstLoadedMap.MapName);
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

                    Logger.Info($"... copying {fileName} -> {fullTargetFilePath}");
                    File.Copy(fileName, fullTargetFilePath, true);
                }
            }

            return true;
        }

        /// <summary>
        /// Deletes all files in the Content/Art/Env/NYC folder
        /// that does not have NYC_01 prefix (original game files).
        /// Also deletes NYC01_Persistent files
        /// </summary>
        /// <remarks>
        /// The NYC01_Persistent.umap file must be deleted so a custom map can be loaded when you leave the apartment in-game.
        /// If this file is not deleted then the game loads the default map when you leave the apartment.
        /// </remarks>
        public void DeleteMapFilesFromNYCFolder()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return;
            }

            foreach (string fileName in Directory.GetFiles(SessionPath.ToNYCFolder))
            {
                // only delete custom map files, not NYC01 files
                if (fileName.Contains("NYC01_") == false)
                {
                    Logger.Info($"... deleting {fileName}");
                    File.Delete(fileName);
                }
            }

            // only delete the .umap file when the game is running, otherwise the default map will always load when you leave the apartment
            // ... Some maps rely on the .umap file to stream other content to the custom map so the .umap file is NOT deleted while Session is not running allowing the custom map to use its assets.
            string nycMapFilePath = Path.Combine(SessionPath.ToNYCFolder, "NYC01_Persistent.umap");

            if (SessionPath.IsSessionRunning() && File.Exists(nycMapFilePath))
            {
                Logger.Info($"... Session running - deleting {nycMapFilePath}");
                File.Delete(nycMapFilePath);
            }
        }

        public string GetGameDefaultMapSetting()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return "";
            }

            try
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData iniFile = parser.ReadFile(SessionPath.ToDefaultEngineIniFile);
                return iniFile["/Script/EngineSettings.GameMapsSettings"]["GameDefaultMap"];
            }
            catch (Exception)
            {
                return "";
            }
        }

        public BoolWithMessage LoadMap(MapListItem map)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Cannot Load: 'Path to Session' is invalid.");
            }

            if (SessionPath.IsSessionRunning() == false || FirstLoadedMap == null)
            {
                FirstLoadedMap = map;
            }

            if (map.IsDefaultMap)
            {
                return LoadDefaultMap(map);
            }

            try
            {
                // delete session map file / custom maps from game 
                DeleteMapFilesFromNYCFolder();

                CopyMapFilesToNYCFolder(map);

                // update the ini file with the new map path
                string selectedMapPath = "/Game/Art/Env/NYC/" + map.MapName;
                SetGameDefaultMapSetting(selectedMapPath);

                return BoolWithMessage.True($"{map.MapName} Loaded!");
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return BoolWithMessage.False($"Failed to load {map.MapName}: {e.Message}");
            }
        }

        public BoolWithMessage LoadDefaultMap(MapListItem map)
        {
            try
            {
                DeleteMapFilesFromNYCFolder();

                string fileNamePrefix = "NYC01_Persistent";
                string[] fileExtensions = { ".umap", ".uexp", "_BuiltData.uasset", "_BuiltData.uexp", "_BuiltData.ubulk" };

                // copy NYC01_Persistent backup files back to original game location
                foreach (string fileExt in fileExtensions)
                {
                    string fullPath = Path.Combine(SessionPath.ToOriginalSessionMapFiles, $"{fileNamePrefix}{fileExt}");
                    string targetPath = Path.Combine(SessionPath.ToNYCFolder, $"{fileNamePrefix}{fileExt}");

                    Logger.Info($"Copying {fullPath} -> {targetPath}");
                    File.Copy(fullPath, targetPath, true);
                }

                SetGameDefaultMapSetting(map.GameDefaultMapSetting, map.GlobalDefaultGameModeSetting);

                return BoolWithMessage.True($"{map.MapName} Loaded!");
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return BoolWithMessage.False($"Failed to load {map.MapName} : {e.Message}");
            }
        }

        public bool SetGameDefaultMapSetting(string defaultMapValue, string defaultGameModeValue = "")
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            var parser = new FileIniDataParser();
            parser.Parser.Configuration.AllowDuplicateKeys = true;
            IniData iniFile = parser.ReadFile(SessionPath.ToDefaultEngineIniFile);
            iniFile["/Script/EngineSettings.GameMapsSettings"]["GameDefaultMap"] = defaultMapValue;

            if (!string.IsNullOrEmpty(defaultGameModeValue))
            {
                iniFile["/Script/EngineSettings.GameMapsSettings"]["GlobalDefaultGameMode"] = defaultGameModeValue;
            }
            else if (iniFile["/Script/EngineSettings.GameMapsSettings"].ContainsKey("GlobalDefaultGameMode"))
            {
                iniFile["/Script/EngineSettings.GameMapsSettings"].RemoveKey("GlobalDefaultGameMode");
            }

            try
            {
                parser.WriteFile(SessionPath.ToDefaultEngineIniFile, iniFile);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public BoolWithMessage BackupOriginalMapFiles()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Cannot backup: 'Path to Session' is invalid.");
            }

            if (IsOriginalMapFilesBackedUp())
            {
                // the files are already backed up
                return BoolWithMessage.False("Skipping backup: original files already backed up.");
            }

            if (DoesOriginalMapFileExistInGameDirectory() == false)
            {
                // the original files are missing from the Session directory
                return BoolWithMessage.False("Cannot backup: original map files for Session are missing from Session game directory.");
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
                    string fullPathToFile = Path.Combine(SessionPath.ToNYCFolder, $"{fileNamePrefix}{fileExt}");
                    string destFilePath = Path.Combine(SessionPath.ToOriginalSessionMapFiles, $"{fileNamePrefix}{fileExt}");
                    File.Copy(fullPathToFile, destFilePath, overwrite: true);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                string errorMsg = $"Failed to backup original map files: {e.Message}";
                return BoolWithMessage.False(errorMsg);
            }

            bool originalMapFilesBackedUp = IsOriginalMapFilesBackedUp();

            DefaultMaps.ForEach(m =>
            {
                m.IsEnabled = originalMapFilesBackedUp;
                m.Tooltip = m.IsEnabled ? null : "The original Session game files have not been backed up to the custom Maps folder.";
            });

            return new BoolWithMessage(IsOriginalMapFilesBackedUp());
        }

        public bool IsOriginalMapFilesBackedUp()
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
                string fullPath = Path.Combine(SessionPath.ToOriginalSessionMapFiles, $"{fileNamePrefix}{fileExt}");
                if (File.Exists(fullPath) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public bool DoesOriginalMapFileExistInGameDirectory()
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
                string fullPath = Path.Combine(SessionPath.ToNYCFolder, $"{fileNamePrefix}{fileExt}");
                if (File.Exists(fullPath) == false)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
