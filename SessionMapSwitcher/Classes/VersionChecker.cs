using NAppUpdate.Framework;
using NAppUpdate.Framework.Tasks;
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    class VersionChecker
    {
        private const string LatestReleaseUrl = "https://github.com/rodriada000/SessionMapSwitcher/releases/latest";

        private const string UpdateFeedUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/release_updates/latest_release/updatefeed.xml";

        private const string _nameOfExe = "SessionMapSwitcher.exe";

        /// <summary>
        /// Get the instance of <see cref="UpdateManager.Instance"/>
        /// </summary>
        public static UpdateManager AppUpdater
        {
            get
            {
                return UpdateManager.Instance;
            }
        }

        public static void OpenLatestReleaseInBrowser()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = LatestReleaseUrl
            };
            Process.Start(startInfo);
        }

        /// <summary>
        /// Makes request to feed.xml url to and check if there is an update available
        /// </summary>
        /// <returns> Returns true if an update is available. </returns>
        public static bool CheckForUpdates()
        {
            try
            {
                AppUpdater.UpdateSource = new NAppUpdate.Framework.Sources.SimpleWebSource(UpdateFeedUrl);
                AppUpdater.CheckForUpdates();

                return HasUpdatesAvailable();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loops over the update tasks and looks for 'SessionMapSwitcher.exe'
        /// </summary>
        /// <returns> true if FileUpdateTask is found with the name 'SessionMapSwitcher.exe' </returns>
        public static bool HasUpdatesAvailable()
        {
            foreach (IUpdateTask task in AppUpdater.Tasks)
            {
                if (task is FileUpdateTask)
                {
                    FileUpdateTask fileTask = (task as FileUpdateTask);
                    if (fileTask.LocalPath == _nameOfExe)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void UpdateApplication()
        {
            if (HasUpdatesAvailable())
            {
                try
                {
                    AppUpdater.PrepareUpdates();
                    AppUpdater.ApplyUpdates(true, true, false);
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show($"An error occurred while trying to update: {e.Message}", "Error Updating!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    AppUpdater.CleanUp();
                }
            }
        }
    }
}
