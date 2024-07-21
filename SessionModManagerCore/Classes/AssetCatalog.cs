using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public enum DownloadLocationType
    {
        INVALID,
        Url,
        MegaFile, // Format: Mega file id e.g. https://mega.nz/file/{ThisIsTheMegaFileID}
        GDrive    // Format: google drive id e.g. https://drive.google.com/file/d/{ThisIsTheDriveID}/view?usp=sharing
    }

    public class AssetCatalog
    {
        public string Name { get; set; }
        public List<Asset> Assets { get; set; }

        private Dictionary<string, Asset> _lookup;

        public AssetCatalog()
        {
            Assets = new List<Asset>();
        }

        public Asset GetAsset(string assetID)
        {
            if (_lookup == null)
            {
                _lookup = Assets.ToDictionary(m => m.ID, m => m);
            }

            Asset mod;
            _lookup.TryGetValue(assetID, out mod);

            return mod;
        }

        public static AssetCatalog Merge(AssetCatalog c1, AssetCatalog c2)
        {
            Dictionary<string, Asset> assets = c1.Assets.ToDictionary(m => m.ID, m => m);

            foreach (var otherAsset in c2.Assets)
            {
                Asset m;
                if (assets.TryGetValue(otherAsset.ID, out m))
                {
                    if (otherAsset.Version > m.Version || otherAsset.UpdatedDate > m.UpdatedDate)
                    {
                        assets[otherAsset.ID] = otherAsset;
                    }
                }
                else
                {
                    assets[otherAsset.ID] = otherAsset;
                }
            }

            return new AssetCatalog() 
            { 
                Name = "Merged Catalog",
                Assets = assets.Values.ToList() 
            };
        }

        /// <summary>
        /// Parses download url information (either http url or google drive) from a rsmm:// download url
        /// </summary>
        /// <param name="link"></param>
        /// <param name="type"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool TryParseDownloadUrl(string link, out DownloadLocationType type, out string url)
        {
            if (link.StartsWith("rsmm://", StringComparison.InvariantCultureIgnoreCase)) link = link.Substring(7);
            string[] parts = link.Split(new[] { '/' }, 2);
            type = DownloadLocationType.INVALID; url = null;

            if (parts.Length < 2) return false;
            if (!Enum.TryParse(parts[0], out type)) return false;

            url = parts[1];
            int dpos = url.IndexOf('$');
            if (dpos >= 0) url = url.Substring(0, dpos) + "://" + url.Substring(dpos + 1);
            return true;
        }

        /// <summary>
        /// Formats a http url into the appropriate rsmm:// url format
        /// </summary>
        internal static string FormatUrl(string subUrl)
        {
            return $"rsmm://Url/{subUrl.Replace("://", "$")}";
        }
    }
}
