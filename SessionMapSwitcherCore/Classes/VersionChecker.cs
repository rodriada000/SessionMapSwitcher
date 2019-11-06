using SessionMapSwitcherCore.Utils;
using System;
using System.Diagnostics;
using System.IO;
using Avantgarde.Core;
using SessionMapSwitcherCore.Classes;

namespace SessionModManagerCore.Classes
{
    /// <summary>
    /// Used to check if a new version of Session Mod Manager is available to download
    /// and handles the app updating process.
    /// </summary>
    public static class VersionChecker
    {
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
                }

                return IsNewVersionAvailable.GetValueOrDefault(false);
            }
            catch (Exception e)
            {
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
                    return BoolWithMessage.False($"An error occurred while trying to update: {e.Message}");
                }
            }

            return BoolWithMessage.True();
        }

    }
}
