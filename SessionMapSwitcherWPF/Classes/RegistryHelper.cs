using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SessionMapSwitcherWPF.Classes
{
    public class RegistryHelper
    {
        public static string GetPathToUnrealEngine()
        {
            string unrealPath = "";
            string registryKeyName = "InstalledDirectory";

            try
            {
                RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\EpicGames\Unreal Engine\4.24");
                string unrealEngineInstallDir = registryKey?.GetValue(registryKeyName).ToString();

                // validate directory exists
                if (String.IsNullOrEmpty(unrealEngineInstallDir) || Directory.Exists(unrealEngineInstallDir) == false)
                {
                    return "";
                }

                return unrealEngineInstallDir;
            }
            catch (Exception)
            {
                // do nothing
            }

            return unrealPath;
        }

        public static string GetPathFromRegistry()
        {
            string sessionPath = string.Empty;
            string registryKeyName = "MatchedExeFullPath";

            try
            {
                RegistryKey regCu = Registry.CurrentUser;
                regCu = regCu.OpenSubKey(@"System\GameConfigStore\Children", true);

                foreach (string Keyname in regCu.GetSubKeyNames())
                {
                    RegistryKey childKey = regCu.OpenSubKey(Keyname);
                    if (childKey.GetValueNames().Contains(registryKeyName))
                    {
                        if (childKey.GetValue(registryKeyName).ToString().EndsWith("SessionGame-Win64-Shipping.exe"))
                        {
                            sessionPath = childKey.GetValue(registryKeyName).ToString();
                            sessionPath = Path.GetDirectoryName(sessionPath);
                            sessionPath = Path.GetFullPath(sessionPath + @"..\..\..\..");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // do nothing
            }

            return sessionPath;
        }

        internal static bool IsSoftwareInstalled(string softwareName, RegistryHive hive, RegistryView registryView)
        {
            string installedProgrammsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            var uninstallKey = RegistryKey.OpenBaseKey(hive, registryView)
                                          .OpenSubKey(installedProgrammsPath);

            if (uninstallKey == null)
                return false;

            return uninstallKey.GetSubKeyNames()
                               .Select(installedSoftwareString => uninstallKey.OpenSubKey(installedSoftwareString))
                               .Select(installedSoftwareKey => installedSoftwareKey.GetValue("DisplayName") as string)
                               .Any(installedSoftwareName => installedSoftwareName != null && installedSoftwareName.Contains(softwareName));
        }

        internal static string GetDisplayVersion(string softwareName, RegistryHive hive, RegistryView registryView)
        {
            string installedProgrammsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            var uninstallKey = RegistryKey.OpenBaseKey(hive, registryView)
                                          .OpenSubKey(installedProgrammsPath);

            if (uninstallKey == null)
                return null;

            return uninstallKey.GetSubKeyNames()
                               .Select(installedSoftwareString => uninstallKey.OpenSubKey(installedSoftwareString))
                               .Select(installedSoftwareKey => new { DisplayName = installedSoftwareKey.GetValue("DisplayName") as string, Key = installedSoftwareKey })
                               .Where(installedSoftware => installedSoftware.DisplayName != null && installedSoftware.DisplayName.Contains(softwareName))
                               .Select(installedSoftware => installedSoftware.Key.GetValue("DisplayVersion") as string)
                               .FirstOrDefault();
        }
        internal static string GetExePathFromDisplayIcon(string softwareName, RegistryHive hive, RegistryView registryView)
        {
            string installedProgrammsPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            var uninstallKey = RegistryKey.OpenBaseKey(hive, registryView)
                                          .OpenSubKey(installedProgrammsPath);

            if (uninstallKey == null)
                return null;

            string exePath = uninstallKey.GetSubKeyNames()
                                         .Select(installedSoftwareString => uninstallKey.OpenSubKey(installedSoftwareString))
                                         .Select(installedSoftwareKey => new { DisplayName = installedSoftwareKey.GetValue("DisplayName") as string, Key = installedSoftwareKey })
                                         .Where(installedSoftware => installedSoftware.DisplayName != null && installedSoftware.DisplayName.Contains(softwareName))
                                         .Select(installedSoftware => installedSoftware.Key.GetValue("DisplayIcon") as string)
                                         .FirstOrDefault();

            if (exePath.Contains(".exe,"))
            {
                int index = exePath.IndexOf(".exe,");
                return exePath.Substring(0, index + 4);
            }

            return exePath;
        }

    }
}
