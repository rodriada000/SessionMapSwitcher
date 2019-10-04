using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    class MetaDataManager
    {
        public const string MetaFolderName = "MapSwitcherMetaData";

        public static string FullPathToMetaFolder
        {
            get
            {
                return $"{SessionPath.ToContent}\\{MetaFolderName}";
            }
        }

        /// <summary>
        /// Recursively searches folders for a .umap file that has the valid Session gamemode and returns the name of it
        /// </summary>
        public static string GetMapFileNameFromFolder(string folder)
        {
            foreach (string fileName in Directory.GetFiles(folder))
            {
                if (fileName.EndsWith(".umap") && MapListItem.HasGameMode(fileName))
                {
                    FileInfo fileInfo = new FileInfo(fileName);
                    return fileInfo.Name;
                }
            }

            foreach (string dir in Directory.GetDirectories(folder))
            {
                string mapName = GetMapFileNameFromFolder(dir);
                if (mapName != "")
                {
                    return mapName;
                }
            }

            return "";
        }

        /// <summary>
        /// Creates a .meta file in the folder 'MapSwitcherMetaData' to store the original import source folder location.
        /// </summary>
        internal static BoolWithMessage TrackMapLocation(string mapName, string sourceFolderToCopy)
        {
            string umapExt = ".umap";

            try
            {
                if (mapName.EndsWith(umapExt))
                {
                    mapName = mapName.Substring(0, mapName.Length - umapExt.Length);
                }

                CreateMetaDataFolder();

                string trackingFileName = Path.Combine(FullPathToMetaFolder, $".meta_{mapName}");
                File.WriteAllText(trackingFileName, sourceFolderToCopy);
                return new BoolWithMessage(true);
            }
            catch (Exception e)
            {
                return new BoolWithMessage(false, e.Message);
            }
        }

        internal static string GetOriginalImportLocation(string mapName)
        {
            try
            {
                string fullMetaDataPath = $"{SessionPath.ToContent}\\{MetaFolderName}";
                string trackingFileName = Path.Combine(fullMetaDataPath, $".meta_{mapName}");

                return File.ReadAllText(trackingFileName);
            }
            catch (Exception)
            {
                return "";
            }
        }

        internal static bool IsImportLocationStored(string mapName)
        {
            return GetOriginalImportLocation(mapName) != "";
        }

        /// <summary>
        /// Creates a file 'customNames.meta' if it does not exist and writes
        /// the custom names of maps to the file.
        /// </summary>
        /// <returns> true if file updated; false if exception thrown </returns>
        /// <remarks>
        /// The map directory and map name is used as the Key to the custom name and is written to the file like so:
        /// MapDirectory | MapName | CustomName | IsHidden
        /// </remarks>
        internal static bool WriteCustomMapPropertiesToFile(IEnumerable<MapListItem> maps)
        {
            try
            {
                CreateMetaDataFolder();

                List<string> linesToWrite = new List<string>();

                foreach (MapListItem map in maps)
                {
                    // only write maps to meta data file that users have set custom properties for
                    if (String.IsNullOrWhiteSpace(map.CustomName) == false || map.IsHiddenByUser)
                    {
                        linesToWrite.Add(map.MetaData);
                    }
                }

                string pathToMetaFile = $"{FullPathToMetaFolder}\\customNames.meta";

                if (File.Exists(pathToMetaFile))
                {
                    File.Delete(pathToMetaFile);
                }

                File.WriteAllLines(pathToMetaFile, linesToWrite.ToArray());
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the custom map names from 'customNames.meta' file and updates
        /// list of maps with their custom names.
        /// </summary>
        /// <param name="maps"></param>
        /// <remarks>
        /// customNames.meta uses the map directory and the map name as the Key to find the correct custom map name
        /// </remarks>
        internal static void SetCustomPropertiesForMaps(IEnumerable<MapListItem> maps)
        {
            try
            {
                string pathToMetaFile = $"{FullPathToMetaFolder}\\customNames.meta";

                if (File.Exists(pathToMetaFile) == false)
                {
                    return;
                }

                foreach (string line in File.ReadAllLines(pathToMetaFile))
                {
                    string[] parts = line.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    string dirPath = parts[0].Trim();
                    string mapName = parts[1].Trim();
                    string customName = parts[2].Trim();
                    string isHidden = parts[3].Trim();


                    MapListItem foundMap = maps.Where(m => m.DirectoryPath == dirPath && m.MapName == mapName).FirstOrDefault();
                    
                    if (foundMap != null)
                    {
                        foundMap.CustomName = customName;
                        foundMap.IsHiddenByUser = (isHidden.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }


        /// <summary>
        /// Creates folder to Meta data folder if it does not exists
        /// </summary>
        internal static void CreateMetaDataFolder()
        {
            if (Directory.Exists(FullPathToMetaFolder) == false)
            {
                Directory.CreateDirectory(FullPathToMetaFolder);
            }
        }
    }
}
