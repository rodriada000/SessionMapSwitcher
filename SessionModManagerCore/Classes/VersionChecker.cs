using SessionMapSwitcherCore.Utils;
using System;
using System.Diagnostics;
using System.IO;
using Avantgarde.Core;
using SessionMapSwitcherCore.Classes;
using System.Collections.Generic;

namespace SessionModManagerCore.Classes
{
    /// <summary>
    /// Used to check if a new version of Session Mod Manager is available to download
    /// and handles the app updating process.
    /// </summary>
    public static class VersionChecker
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static Updater _updater;

        /// <summary>
        /// Static instance of <see cref="_updater"/>.
        /// Gets initialized if null.
        /// </summary>
        public static Updater UpdaterInstance
        {
            get
            {
                if (_updater == null)
                {
                    _updater = new Updater(AppContext.BaseDirectory);
                }

                return _updater;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool? IsNewVersionAvailable = null;

        /// <summary>
        /// If <see cref="IsNewVersionAvailable"/> is null then makes request to ag_files.json url to check if there is an update available and sets <see cref="IsNewVersionAvailable"/>.
        /// Returns <see cref="IsNewVersionAvailable"/>.
        /// </summary>
        /// <returns> Returns true if an update is available. </returns>
        public static bool IsUpdateAvailable()
        {
            try
            {
                if (IsNewVersionAvailable.HasValue == false)
                {
                    IsNewVersionAvailable = UpdaterInstance.CheckForUpdates();
                    DeleteOldVersionFiles();
                }

                return IsNewVersionAvailable.GetValueOrDefault(false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to check for updates");
                return false;
            }
        }

        /// <summary>
        /// Runs avantgarde updater which will exit the application to run a seperate console app that downloads
        /// and copies the latest files.
        /// </summary>
        public static BoolWithMessage UpdateApplication()
        {
            if (IsUpdateAvailable())
            {
                try
                {
                    UpdaterInstance.Update();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "failed to launch update process");
                    return BoolWithMessage.False($"An error occurred while trying to update: {e.Message}");
                }
            }

            return BoolWithMessage.True();
        }


        #region Methods Related to Cleaning up from versions < 2.2.0.0

        /// <summary>
        /// List of files that are used in version older than 2.2.0.0
        /// These will be deleted if they exist in the app directory
        /// </summary>
        private static List<string> _filesToDelete = new List<string>() { "NAppUpdate.Framework.dll", "SessionMapSwitcher.exe", "SessionMapSwitcher.exe.config" };

        /// <summary>
        /// Delete files used in versions older than 2.2.0.0
        /// </summary>
        private static void DeleteOldVersionFiles()
        {
            // check if SessionMapSwitcher.exe process is running
            string currentProcName = Process.GetCurrentProcess().ProcessName;

            if (currentProcName.Contains("SessionMapSwitcher"))
            {
                // We do not want to delete the extra files until we finish updating 2.2.0.0 by downloading the SessionModManager.exe file and relaunching using that proc
                // ... we download the newly named .exe by running the update process (the version is set to 2.1.5.0 in ag_settings.json to trigger an update)
                // ... note that the .exes are the same except for the name
                return;
            }

            foreach (string filename in _filesToDelete)
            {
                string fullPath = Path.Combine(AppContext.BaseDirectory, filename);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        #endregion

    }
}
