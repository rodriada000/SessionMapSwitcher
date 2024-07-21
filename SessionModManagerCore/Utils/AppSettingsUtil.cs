using System;
using System.Configuration;

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
    }

    public class AppSettingsUtil
    {
        public static void AddOrUpdateAppSettings(SettingKey key, string value)
        {
            AddOrUpdateAppSettings(key.ToString(), value);
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                KeyValueConfigurationCollection settings = configFile.AppSettings.Settings;

                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
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
                Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                KeyValueConfigurationCollection settings = configFile.AppSettings.Settings;

                if (settings[key] == null)
                {
                    return "";
                }
                else
                {
                    return settings[key].Value;
                }
            }
            catch (ConfigurationErrorsException)
            {
                return "";
            }
        }
    }
}
