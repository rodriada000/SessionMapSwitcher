using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SessionMapSwitcherCore.Classes
{
    public static class SessionPath
    {
        public const string MapBackupFolderName = "Original_Session_Map";
        public const string GameTexturesJsonFileName = "game_textures.json";

        public static string ToApplicationRoot
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string ToApplicationResourcesFolder
        {
            get
            {
                return Path.Combine(ToApplicationRoot, "Resources");
            }
        }

        private static string _toSession = "";
        public static string ToSession
        {
            get
            {
                if (_toSession.EndsWith("\\"))
                {
                    _toSession = _toSession.TrimEnd('\\');
                }

                if (_toSession.EndsWith("/"))
                {
                    _toSession = _toSession.TrimEnd('/');
                }

                if (string.IsNullOrEmpty(_toSession))
                {
                    return "";
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
                return Path.Combine(ToSession, "SessionGame");
            }
        }

        public static string ToContent
        {
            get
            {
                return Path.Combine(ToSessionGame, "Content");
            }
        }

        public static string ToBinariesWin64
        {
            get
            {
                return Path.Combine(ToSessionGame, "Binaries", "Win64");
            }
        }

        public static string ToConfig
        {
            get
            {
                return Path.Combine(ToSessionGame, "Config");
            }
        }

        public static string ToPaks
        {
            get
            {
                return Path.Combine(ToContent, "Paks");
            }
        }

        public static string ToMovies
        {
            get
            {
                return Path.Combine(ToContent, "Movies");
            }
        }

        public static string ToPakFile
        {
            get
            {
                if (File.Exists(Path.Combine(ToPaks, "pakchunk0-WindowsNoEditor.pak"))) // they changed the .pak file in 0.0.0.7
                {
                    return Path.Combine(ToPaks, "pakchunk0-WindowsNoEditor.pak");
                }

                return Path.Combine(ToPaks, "SessionGame-WindowsNoEditor.pak");
            }
        }

        public static string ToCryptoJsonFile
        {
            get
            {
                return Path.Combine(ToPaks, "crypto.json");
            }
        }


        public static string ToDefaultEngineIniFile
        {
            get
            {
                return Path.Combine(ToConfig, "DefaultEngine.ini");
            }
        }

        public static string ToUserEngineIniFile
        {
            get
            {
                return Path.Combine(ToConfig, "UserEngine.ini");
            }
        }

        public static string ToDefaultGameIniFile
        {
            get
            {
                return Path.Combine(ToConfig, "DefaultGame.ini");
            }
        }

        /// <summary>
        /// Returns absolute path to the NYC folder in Session game directory. Requires <see cref="SessionPath"/>.
        /// </summary>
        public static string ToNYCFolder
        {
            get
            {
                return Path.Combine(new string[] { ToContent, "Art", "Env", "NYC" });
            }
        }

        public static string ToOriginalSessionMapFiles
        {
            get
            {
                return Path.Combine(ToContent, MapBackupFolderName);
            }
        }

        public static string ToSessionExe
        {
            get
            {
                return Path.Combine(ToBinariesWin64, "SessionGame-Win64-Shipping.exe");
            }
        }

        public static string ToSaveGamesFolder
        {
            get
            {
                return Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SessionGame", "Saved", "SaveGames" });
            }
        }

        public static string ToTutorialsSaveSlotFile
        {
            get
            {
                return Path.Combine(ToSaveGamesFolder, "TutorialsSaveSlot.sav");
            }
        }

        public static string ToLocalAppDataConfigFolder
        {
            get
            {
                return Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SessionGame", "Saved", "Config", "WindowsNoEditor" });
            }
        }

        public static string FullPathToMetaFolder
        {
            get
            {
                return Path.Combine(ToContent, "MapSwitcherMetaData");
            }
        }

        public static string FullPathToMetaImagesFolder
        {
            get
            {
                return Path.Combine(FullPathToMetaFolder, "images");
            }
        }

        public static string ToMetaGameTexturesJsonFile
        {
            get
            {
                return Path.Combine(FullPathToMetaFolder, GameTexturesJsonFileName);
            }
        }


        public static bool IsSessionPathValid()
        {
            if (String.IsNullOrEmpty(ToSession))
            {
                return false;
            }

            if (Directory.Exists(Path.Combine(ToSession, "Engine")) == false)
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

        public static bool IsSessionRunning()
        {
            var allProcs = Process.GetProcessesByName("SessionGame-Win64-Shipping");

            return allProcs.Length > 0;
        }
    }
}
