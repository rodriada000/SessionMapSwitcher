using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    class MapImporter
    {
        public const string MetaFolderName = "MapSwitcherMetaData";

        /// <summary>
        /// Recursively searches folders for a .umap file and returns the name of it
        /// </summary>
        public static string GetMapFileNameFromFolder(string folder)
        {
            foreach (string fileName in Directory.GetFiles(folder))
            {
                if (fileName.EndsWith(".umap"))
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
        internal static BoolWithMessage TrackMapLocation(string mapName, string sourceFolderToCopy, string trackingFileLocation)
        {
            string umapExt = ".umap";

            try
            {
                if (mapName.EndsWith(umapExt))
                {
                    mapName = mapName.Substring(0, mapName.Length - umapExt.Length);
                }

                string fullMetaDataPath = $"{trackingFileLocation}\\{MetaFolderName}";

                if (Directory.Exists(fullMetaDataPath) == false)
                {
                    Directory.CreateDirectory(fullMetaDataPath);
                }

                string trackingFileName = Path.Combine(fullMetaDataPath, $".meta_{mapName}");
                File.WriteAllText(trackingFileName, sourceFolderToCopy);
                return new BoolWithMessage(true);
            }
            catch (Exception e)
            {
                return new BoolWithMessage(false, e.Message);
            }
        }

        internal static string GetOriginalImportLocation(string displayName, string sessionContentPath)
        {
            try
            {
                string fullMetaDataPath = $"{sessionContentPath}\\{MetaFolderName}";
                string trackingFileName = Path.Combine(fullMetaDataPath, $".meta_{displayName}");

                return File.ReadAllText(trackingFileName);
            }
            catch (Exception)
            {
                return "";
            }
        }

        internal static bool IsImportLocationStored(string sessionContentPath, string displayName)
        {
            return GetOriginalImportLocation(displayName, sessionContentPath) != "";
        }
    }
}
