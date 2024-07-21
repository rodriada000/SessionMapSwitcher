using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.Classes
{
    /// <summary>
    /// Class to provide methods for patching the game with Illusory UE Universal Mod Unlocker
    /// </summary>
    public class UeModUnlocker
    {
        /// <summary>
        /// Github link to .json file that contains the latest download link to the Universal Mod Unlocker
        /// </summary>
        private const string ModUnlockerGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/modUnlockerDownloadLink.json";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static ModUnlockerDownloadLink GetLatestDownloadLinkInfo()
        {
            ModUnlockerDownloadLink link = null;

            try
            {
                // visit github to get current anon file download link
                Logger.Info("Getting latest download link info from github ...");

                string jsonContent = DownloadUtils.GetTextResponseFromUrl(ModUnlockerGitHubUrl);

                link = Newtonsoft.Json.JsonConvert.DeserializeObject<ModUnlockerDownloadLink>(jsonContent);
            }
            catch (AggregateException e)
            {
                Logger.Error(e);
                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }

            return link;
        }




        /// <summary>
        /// Checks if mod unlocker has been ran by looking for the dxgi .dll in Binaries\Win64
        /// </summary>
        public static bool IsGamePatched()
        {
            return File.Exists(Path.Combine(SessionPath.ToBinariesWin64, "dxgi.dll"));
        }

    }

    public class ModUnlockerDownloadLink
    {
        public decimal Version { get; set; }
        public string Url { get; set; }
    }
}
