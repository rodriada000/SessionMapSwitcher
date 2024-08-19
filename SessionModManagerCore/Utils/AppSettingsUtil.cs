using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;

namespace SessionMapSwitcherCore.Utils
{
    public enum SettingKey
    {
        PathToSession,
        ShowInvalidMaps,
        ProjectWatcherPath,
        CustomWindowSize,
        FetchAllPreviewImages,
        DeleteDownloadAfterAssetInstall,
        AssetStoreMapsChecked,
        AssetStoreDecksChecked,
        AssetStoreGriptapesChecked,
        AssetStoreTrucksChecked,
        AssetStoreWheelsChecked,
        AssetStoreHatsChecked,
        AssetStorePantsChecked,
        AssetStoreShirtsChecked,
        AssetStoreShoesChecked,
        AssetStoreMeshesChecked,
        AssetStoreCharactersChecked,
        WindowState,
        EnableRMSTools,
        LastSelectedMap,
        LaunchViaSteam,
        AppTheme,
        AllowModConflicts,
    }

    public class AppSettingsUtil
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static Dictionary<string, string> _config = null;

        public static void AddOrUpdateAppSettings(SettingKey key, string value)
        {
            AddOrUpdateAppSettings(key.ToString(), value);
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                string settingsPath = Path.Combine(SessionPath.ToApplicationRoot, "appsettings.json");

                if (_config == null)
                {
                    string configJson = File.ReadAllText(settingsPath);
                    _config = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);
                }

                if (!_config.ContainsKey(key))
                {
                    _config.Add(key, value);
                }
                else
                {
                    _config[key] = value;
                }

                File.WriteAllText(settingsPath, JsonConvert.SerializeObject(_config));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error writing app settings");
            }
        }

        public static string GetAppSetting(SettingKey key)
        {
            return GetAppSetting(key.ToString());
        }

        public static string GetAppSetting(string key)
        {
            try
            {

                if (_config == null)
                {
                    string settingsPath = Path.Combine(SessionPath.ToApplicationRoot, "appsettings.json");
                    string configJson = File.ReadAllText(settingsPath);
                    _config = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);
                }

                if (_config.ContainsKey(key))
                {
                    return _config[key];
                }

                return "";
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error writing app settings");
                return "";
            }
        }
    }
}
