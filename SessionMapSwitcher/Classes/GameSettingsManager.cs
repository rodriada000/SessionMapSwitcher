using Ini.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    public static class GameSettingsManager
    {
        private static double _gravity;
        public static double Gravity { get => _gravity; set => _gravity = value; }

        public static bool SkipIntroMovie { get; set; }

        public static int ObjectCount { get; set; }

        public static string PathToObjectPlacementFile
        {
            get
            {
                return $"{SessionPath.ToContent}\\ObjectPlacement\\Blueprints\\PBP_ObjectPlacementInventory.uexp";
            }
        }

        public static BoolWithMessage RefreshGameSettingsFromIniFiles()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            try
            {
                IniFile engineFile = new IniFile(SessionPath.ToUserEngineIniFile);
                string gravitySetting = engineFile.ReadString("/Script/Engine.PhysicsSettings", "DefaultGravityZ");

                if (String.IsNullOrWhiteSpace(gravitySetting))
                {
                    gravitySetting = "-980";
                }

                double.TryParse(gravitySetting, out _gravity);

                IniFile gameFile = new IniFile(SessionPath.ToDefaultGameIniFile);
                SkipIntroMovie = gameFile.ReadBoolean("/Script/UnrealEd.ProjectPackagingSettings", "bSkipMovies");

                GetObjectCountFromFile();

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                Gravity = -980;
                SkipIntroMovie = false;

                return BoolWithMessage.False($"Could not get game settings: {e.Message}");
            }
        }

        /// <summary>
        /// writes the game settings to the correct files.
        /// </summary>
        /// <returns> true if settings updated; false otherwise. </returns>
        public static BoolWithMessage WriteGameSettingsToFile(string gravityText, string objectCountText, bool skipMovie)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            // remove trailing 0's from float value for it to parse correctly
            int indexOfDot = gravityText.IndexOf(".");
            if (indexOfDot >= 0)
            {
                gravityText = gravityText.Substring(0, indexOfDot);
            }

            if (float.TryParse(gravityText, out float gravityFloat) == false)
            {
                return BoolWithMessage.False("Invalid Gravity setting.");
            }

            if (int.TryParse(objectCountText, out int parsedObjCount) == false)
            {
                return BoolWithMessage.False("Invalid Object Count setting.");
            }

            if (parsedObjCount <= 0 || parsedObjCount > 65535)
            {
                return BoolWithMessage.False("Object Count must be between 0 and 65535.");
            }


            try
            {
                BoolWithMessage didSetCount = SetObjectCountInFile(objectCountText);

                if (didSetCount.Result == false)
                {
                    return didSetCount;
                }

                IniFile engineFile = new IniFile(SessionPath.ToUserEngineIniFile);
                engineFile.WriteString("/Script/Engine.PhysicsSettings", "DefaultGravityZ", gravityText);

                IniFile gameFile = new IniFile(SessionPath.ToDefaultGameIniFile);

                if (skipMovie)
                {
                    // delete the two StartupMovies from .ini
                    if (gameFile.KeyExists("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies"))
                    {
                        gameFile.DeleteKey("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies");
                    }
                    if (gameFile.KeyExists("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies"))
                    {
                        gameFile.DeleteKey("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies");
                    }
                }
                else
                {
                    if (gameFile.KeyExists("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies") == false)
                    {
                        gameFile.WriteString("/Script/MoviePlayer.MoviePlayerSettings", "+StartupMovies", "UE4_Moving_Logo_720\n+StartupMovies=IntroLOGO_720_30");
                    }
                }

                gameFile.WriteString("/Script/UnrealEd.ProjectPackagingSettings", "bSkipMovies", skipMovie.ToString());

                Gravity = gravityFloat;
                SkipIntroMovie = skipMovie;
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to update game settings: {e.Message}");
            }



            return BoolWithMessage.True();
        }


        /// <summary>
        /// Get the Object Placement count from the file (only reads the first address) and set <see cref="ObjectCount"/>
        /// </summary>
        internal static BoolWithMessage GetObjectCountFromFile()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            try
            {
                using (var stream = new FileStream(PathToObjectPlacementFile, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = 351;
                    int byte1 = stream.ReadByte();
                    int byte2 = stream.ReadByte();
                    byte[] byteArray;

                    // convert two bytes to a hex string. if the second byte is less than 16 than swap the bytes due to reasons....
                    if (byte2 == 0)
                    {
                        byteArray = new byte[] { 0x00, Byte.Parse(byte1.ToString()) };
                    }
                    else if (byte2 < 16)
                    {
                        byteArray = new byte[] { Byte.Parse(byte2.ToString()), Byte.Parse(byte1.ToString()) };
                    }
                    else
                    {
                        byteArray = new byte[] { Byte.Parse(byte1.ToString()), Byte.Parse(byte2.ToString()) };
                    }
                    string hexString = BitConverter.ToString(byteArray).Replace("-", "");

                    // convert the hex string to base 10 int value
                    ObjectCount = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);

                    return BoolWithMessage.True();
                }
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to get object count: {e.Message}");
            }
        }

        /// <summary>
        /// Updates the PBP_ObjectPlacementInventory.uexp file with the new object count value (every placeable object is updated with new count).
        /// This works by converting <see cref="ObjectCountText"/> to bytes and writing the bytes to specific addresses in the file.
        /// </summary>
        internal static BoolWithMessage SetObjectCountInFile(string objectCountText)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            // this is a list of addresses where the item count for placeable objects are stored in the .uexp file
            // ... if this file is modified then these addresses will NOT match so it is important to not mod/change the PBP_ObjectPlacementInventory file (until further notice...)
            List<int> addresses = new List<int>() { 351, 615, 681, 747, 879, 945, 1011, 1077, 1143, 1209, 1275, 1341, 1407, 1473, 1605 };

            try
            {
                using (var stream = new FileStream(PathToObjectPlacementFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    // convert the base 10 int into a hex string (e.g. 10 => 'A' or 65535 => 'FF')
                    string hexValue = int.Parse(objectCountText).ToString("X");

                    // convert the hext string into a byte array that will be written to the file
                    byte[] bytes = StringToByteArray(hexValue);

                    if (hexValue.Length == 3)
                    {
                        // swap bytes around for some reason when the hex string is only 3 characters long... big-endian little-endian??
                        byte temp = bytes[1];
                        bytes[1] = bytes[0];
                        bytes[0] = temp;
                    }

                    // loop over every address so every placeable object is updated with new item count
                    foreach (int fileAddress in addresses)
                    {
                        stream.Position = fileAddress;
                        stream.WriteByte(bytes[0]);

                        // when object count is less than 16 than the byte array will only have 1 byte so write null in next byte position
                        if (bytes.Length > 1)
                        {
                            stream.WriteByte(bytes[1]);
                        }
                        else
                        {
                            stream.WriteByte(0x00);
                        }
                    }

                    stream.Flush(); // ensure file is written to
                }

                ObjectCount = int.Parse(objectCountText); // set in-memory setting to new value written to file
                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to set object count: {e.Message}");
            }
        }

        private static byte[] StringToByteArray(String hex)
        {
            // reference: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa

            if (hex.Length % 2 != 0)
            {
                // pad with '0' for odd length strings like 'A' so it becomes '0A' or '1A4' => '01A4'
                hex = '0' + hex;
            }

            int numChars = hex.Length;
            byte[] bytes = new byte[numChars / 2];
            for (int i = 0; i < numChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }


        public static bool UpdateGameDefaultMapIniSetting(string defaultMapValue)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            IniFile iniFile = new IniFile(SessionPath.ToUserEngineIniFile);
            return iniFile.WriteString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap", defaultMapValue);
        }

        public static string GetGameDefaultMapIniSetting()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return "";
            }

            try
            {
                IniFile iniFile = new IniFile(SessionPath.ToDefaultEngineIniFile);
                return iniFile.ReadString("/Script/EngineSettings.GameMapsSettings", "GameDefaultMap");
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static bool DoesSettingsFileExist()
        {
            return File.Exists(PathToObjectPlacementFile) && File.Exists(SessionPath.ToDefaultGameIniFile);
        }
    }
}
