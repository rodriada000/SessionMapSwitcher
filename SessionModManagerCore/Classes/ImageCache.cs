using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class ImageCache
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const string cacheFileName = "imageCache.json";

        public static string PathToCacheFile
        {
            get
            {
                return Path.Combine(AssetStoreViewModel.AbsolutePathToStoreData, cacheFileName);
            }
        }

        private static ImageCache _instance;
        public static ImageCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    LoadFromFile();
                }

                return _instance;
            }
        }

        private Dictionary<string, ImageCacheEntry> _cacheEntries;

        public Dictionary<string, ImageCacheEntry> CacheEntries
        {
            get
            {
                if (_cacheEntries == null)
                {
                    _cacheEntries = new Dictionary<string, ImageCacheEntry>();
                }

                return _cacheEntries;
            }
            set
            {
                _cacheEntries = value;
            }
        }

        public static bool IsOutOfDate(string fileName)
        {
            if (!Instance.CacheEntries.ContainsKey(fileName))
            {
                return true;
            }

            if (!File.Exists(fileName) && !HasCustomFilePath(fileName))
            {
                return true;
            }

            return DateTime.Now.Subtract(Instance.CacheEntries[fileName].LastUpdated).TotalDays > 3;
        }

        public static bool IsSourceUrlDifferent(string fileName, string sourceUrl)
        {
            if (!Instance.CacheEntries.ContainsKey(fileName))
            {
                return true;
            }

            return !Instance.CacheEntries[fileName].SourceUrl.Equals(sourceUrl, StringComparison.InvariantCultureIgnoreCase);
        }

        public static ImageCacheEntry Get(string fileName)
        {
            Instance.CacheEntries.TryGetValue(fileName, out ImageCacheEntry foundEntry);
            return foundEntry;
        }

        public static ImageCacheEntry AddOrUpdate(string fileName, string sourceUrl)
        {
            if (Instance.CacheEntries.ContainsKey(fileName))
            {
                Instance.CacheEntries[fileName].SourceUrl = sourceUrl;
                Instance.CacheEntries[fileName].LastUpdated = DateTime.Now;
                return Instance.CacheEntries[fileName];
            }

            Instance.CacheEntries.Add(fileName, new ImageCacheEntry()
            {
                FilePath = fileName,
                LastUpdated = DateTime.Now,
                SourceUrl = sourceUrl
            });

            return Instance.CacheEntries[fileName];
        }

        public static bool Remove(string fileName)
        {
            if (Instance.CacheEntries.ContainsKey(fileName))
            {
                return Instance.CacheEntries.Remove(fileName);
            }

            return false;
        }

        public static bool WriteToFile()
        {
            try
            {
                string jsonToSave = JsonConvert.SerializeObject(Instance, Formatting.Indented);
                File.WriteAllText(PathToCacheFile, jsonToSave);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        public static bool LoadFromFile()
        {
            try
            {
                if (File.Exists(PathToCacheFile) == false)
                {
                    _instance = new ImageCache();
                    return true;
                }

                string fileContents = File.ReadAllText(PathToCacheFile);
                _instance = JsonConvert.DeserializeObject<ImageCache>(fileContents);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);

                _instance = new ImageCache();
                return false;
            }
        }

        /// <summary>
        /// returns true if image cache is pointing to installed mod preview_thumbnail file
        /// </summary>
        /// <param name="pathToThumbnail"></param>
        /// <returns></returns>
        internal static bool HasCustomFilePath(string pathToThumbnail)
        {
            if (!Instance.CacheEntries.ContainsKey(pathToThumbnail))
            {
                return false;
            }

            return !Instance.CacheEntries[pathToThumbnail].FilePath.Equals(pathToThumbnail, StringComparison.InvariantCultureIgnoreCase) && File.Exists(Instance.CacheEntries[pathToThumbnail].FilePath);
        }
    }


    public class ImageCacheEntry
    {
        public string FilePath { get; set; }

        public DateTime LastUpdated { get; set; }

        public string SourceUrl { get; set; }
    }
}
