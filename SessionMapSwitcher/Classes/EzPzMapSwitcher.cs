using Ini.Net;
using SessionMapSwitcher.Classes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    /// <summary>
    /// Class to handle loading maps for EzPz patched games.
    /// </summary>
    class EzPzMapSwitcher : IMapSwitcher
    {
        public MapListItem DefaultSessionMap { get; }
        internal MapListItem FirstLoadedMap { get; set; }

        public EzPzMapSwitcher()
        {
            DefaultSessionMap = new MapListItem()
            {
                FullPath = SessionPath.ToOriginalSessionMapFiles,
                MapName = "Session Default Map - Brooklyn Banks"
            };

        }
        public MapListItem GetDefaultSessionMap()
        {
            return DefaultSessionMap;
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


                    if (SessionPath.IsSessionRunning())
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

            if (Directory.Exists(SessionPath.ToNYCFolder) == false)
            {
                Directory.CreateDirectory(SessionPath.ToNYCFolder);
            }

            if (map == DefaultSessionMap)
            {
                return LoadOriginalMap();
            }

            try
            {
                // delete session map file / custom maps from game 
                DeleteMapFilesFromNYCFolder();

                CopyMapFilesToNYCFolder(map);

                // update the ini file with the new map path
                // .. when the game is running the map file is renamed to NYC01_Persistent so it can load when you leave the apartment
                string selectedMapPath = "/Game/Art/Env/NYC/NYC01_Persistent";

                if (SessionPath.IsSessionRunning() == false)
                {
                    selectedMapPath = $"/Game/Art/Env/NYC/{map.MapName}";
                }

                SetGameDefaultMapSetting(selectedMapPath);


                return BoolWithMessage.True($"{map.MapName} Loaded!");
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to load {map.MapName}: {e.Message}");
            }
        }

        public BoolWithMessage LoadOriginalMap()
        {
            try
            {
                DeleteMapFilesFromNYCFolder();

                SetGameDefaultMapSetting("/Game/Tutorial/Intro/MAP_EntryPoint");

                return BoolWithMessage.True($"{DefaultSessionMap.MapName} Loaded!");

            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to load Original Session Game Map : {e.Message}");
            }
        }

        /// <summary>
        /// Deletes all files in the Content\Art\Env\NYC folder
        /// </summary>
        public void DeleteMapFilesFromNYCFolder()
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

        /// <summary>
        /// Checks .ini file for the map that will load on game start
        /// </summary>
        public string GetGameDefaultMapSetting()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return "";
            }

            try
            {
                IniFile iniFile = new IniFile(SessionPath.ToUserEngineIniFile);
                return iniFile.ReadString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap");
            }
            catch (Exception)
            {
                return "";
            }
        }


        public bool SetGameDefaultMapSetting(string defaultMapValue)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            IniFile iniFile = new IniFile(SessionPath.ToUserEngineIniFile);
            return iniFile.WriteString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap", defaultMapValue);
        }
    }
}
