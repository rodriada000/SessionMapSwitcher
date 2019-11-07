using Microsoft.Win32;
using System;
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
                RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\EpicGames\Unreal Engine\4.22");
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
    }
}
