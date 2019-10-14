using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    public static class SessionPath
    {
        public const string MapBackupFolderName = "Original_Session_Map";

        private static string _toSession;
        public static string ToSession
        {
            get
            {
                if (_toSession.EndsWith("\\"))
                {
                    _toSession = _toSession.TrimEnd('\\');
                }
                return _toSession;
            }
            set
            {
                _toSession = value;
            }
        }

        public static string ToSessionGame
        {
            get
            {
                return $"{ToSession}\\SessionGame";
            }
        }

        public static string ToContent
        {
            get
            {
                return $"{ToSessionGame}\\Content";
            }
        }

        public static string ToConfig
        {
            get
            {
                return $"{ToSessionGame}\\Config";
            }
        }

        public static string ToPaks
        {
            get
            {
                return $"{ToContent}\\Paks";
            }
        }

        public static string ToMovies
        {
            get
            {
                return $"{ToContent}\\Movies";
            }
        }

        public static string ToPakFile
        {
            get
            {
                return $"{ToContent}\\Paks\\SessionGame-WindowsNoEditor.pak";
            }
        }

        public static string ToCryptoJsonFile
        {
            get
            {
                return $"{ToContent}\\Paks\\crypto.json";
            }
        }


        public static string ToDefaultEngineIniFile
        {
            get
            {
                return $"{ToConfig}\\DefaultEngine.ini";
            }
        }

        public static string ToUserEngineIniFile
        {
            get
            {
                return $"{ToConfig}\\UserEngine.ini";
            }
        }

        public static string ToDefaultGameIniFile
        {
            get
            {
                return $"{ToConfig}\\DefaultGame.ini";
            }
        }

        /// <summary>
        /// Returns absolute path to the NYC folder in Session game directory. Requires <see cref="SessionPath"/>.
        /// </summary>
        public static string ToNYCFolder
        {
            get
            {
                return $"{ToContent}\\Art\\Env\\NYC";
            }
        }

        public static string ToBrooklynFolder
        {
            get
            {
                return $"{ToNYCFolder}\\Brooklyn";
            }
        }

        public static string ToOriginalSessionMapFiles
        {
            get
            {
                return $"{ToContent}\\{MapBackupFolderName}";
            }
        }

        public static string ToSessionExe
        {
            get
            {
                return $"{ToSession}\\SessionGame.exe";
            }
        }

        public static bool IsSessionPathValid()
        {
            if (String.IsNullOrEmpty(ToSession))
            {
                return false;
            }

            if (Directory.Exists($"{ToSession}\\Engine") == false)
            {
                return false;
            }

            if (Directory.Exists(ToSessionGame) == false)
            {
                return false;
            }

            if (Directory.Exists(ToContent) == false)
            {
                return false;
            }

            return true;
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
