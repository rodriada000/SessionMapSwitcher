using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace SessionModManagerCore.Classes
{
    public class RMSToolsuiteLoader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static List<string> _loadedToolsuiteFiles;

        public static List<string> LoadedToolsuiteFiles
        {
            get
            {
                if (_loadedToolsuiteFiles == null)
                {
                    _loadedToolsuiteFiles = new List<string>();
                }
                return _loadedToolsuiteFiles;
            }
            set
            {
                _loadedToolsuiteFiles = value;
            }
        }

        public static string PathToContentArtFolder
        {
            get
            {
                return Path.Combine(SessionPath.ToContent, "Art");
            }
        }

        public static string PathToToolsuite
        {
            get
            {
                return Path.Combine(new string[] { SessionPath.ToContent, "CustomMaps", "RMS", "RMS_Toolsuite" });
            }
        }

        /// <summary>
        /// verify the toolsuite is installed by checking for the directory and making sure the directory is not empty
        /// </summary>
        /// <returns>true if installed</returns>
        public static bool IsToolsuiteInstalled()
        {
            return Directory.Exists(PathToToolsuite) && Directory.GetFiles(PathToToolsuite).Length > 0;
        }

        /// <summary>
        /// Gets the currently loaded RMS files in the Art/Env/NYC folder and returns true if all files are found; false if any file missing
        /// </summary>
        public static bool IsLoaded()
        {
            string pathToDataFolder = Path.Combine(PathToToolsuite, "data");
            List<string> filesThatShouldBeLoaded = FileUtils.GetAllFilesInDirectory(pathToDataFolder);
            string targetPath;
            bool isLoaded = true;

            LoadedToolsuiteFiles.Clear();

            foreach (string file in filesThatShouldBeLoaded)
            {
                targetPath = file.Replace(pathToDataFolder, PathToContentArtFolder);
                if (!File.Exists(targetPath))
                {
                    isLoaded = false;
                }

                LoadedToolsuiteFiles.Add(targetPath);
            }

            return isLoaded;
        }

        /// <summary>
        /// copy required read-only files to 'Content\Art' folder to enable the tool suite
        /// </summary>
        public static void CopyFilesToEnvFolder()
        {
            if (IsLoaded())
            {
                return; // already loaded so don't copy again
            }

            try
            {
                string pathToDataFolder = Path.Combine(PathToToolsuite, "data");

                if (!Directory.Exists(PathToContentArtFolder))
                {
                    Directory.CreateDirectory(PathToContentArtFolder);
                }

                FileUtils.CopyDirectoryRecursively(pathToDataFolder, PathToContentArtFolder);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public static void DeleteFilesInEnvFolder()
        {
            if (!IsLoaded())
            {
                return; // not loaded so no files to delete
            }

            try
            {
                BoolWithMessage result = FileUtils.DeleteFiles(LoadedToolsuiteFiles);

                if (!result.Result)
                {
                    Logger.Warn($"Failed to delete RMS files: {result.Message}");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
