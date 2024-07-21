using Newtonsoft.Json;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class CatalogSettings
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string _defaultCatalogListUrl = "https://raw.githubusercontent.com/rodriada000/SessionCustomMapReleases/master/SMMCatalogList.txt";
        public List<CatalogSubscription> CatalogUrls { get; set; }

        public CatalogSettings()
        {
            CatalogUrls = new List<CatalogSubscription>();
        }

        /// <summary>
        /// Ensures all default catalog urls are in the users catalog settings
        /// </summary>
        internal static void AddDefaults(CatalogSettings settings)
        {
            string txtFile = DownloadUtils.GetTextResponseFromUrl(_defaultCatalogListUrl);

            bool addedDefaults = false;
            List<string> defaultCatalogs = txtFile.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string url in defaultCatalogs)
            {
                if (!settings.CatalogUrls.Any(c => c.Url == url))
                {
                    settings.CatalogUrls.Add(new CatalogSubscription()
                    {
                        Name = GetNameFromAssetCatalog(url),
                        Url = url,
                    });
                    addedDefaults = true;
                }
            }



            if (addedDefaults)
            {
                string contents = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson, contents);
            }
        }

        internal static string GetNameFromAssetCatalog(string url)
        {
            string name = "";

            try
            {
                string catalogStr = DownloadUtils.GetTextResponseFromUrl(url, 5);
                AssetCatalog newCatalog = JsonConvert.DeserializeObject<AssetCatalog>(catalogStr);
                name = newCatalog.Name ?? "";
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Logger.Warn($"Failed to get catalog name from url {url}");
            }

            return name;
        }
    }
}
