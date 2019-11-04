using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class FileUtilsUnitTests
    {
        [TestMethod]
        public void Test_ExtractRarFiles_ExtractsCorrectly()
        {
            string pathToZip = "C:\\Users\\Adam\\Downloads\\Skate3.rar";
            string extractPath = "C:\\Users\\Adam\\Documents\\TestDir";

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            BoolWithMessage result = FileUtils.ExtractRarFile(pathToZip, extractPath);

            Assert.IsTrue(result.Result);

            bool extractedFileExists = File.Exists($"{extractPath}\\Skate3\\BlackBoxPark\\Tex\\1.uasset");
            Assert.IsTrue(extractedFileExists);

            bool mapDirExists = Directory.Exists($"{extractPath}\\Skate3\\Maps");
            Assert.IsTrue(mapDirExists);
        }

        [TestMethod]
        public void Test_ReadBluePrint()
        {
            string PathToObjectPlacementFile = @"C:\Program Files (x86)\Steam\steamapps\common\Session\SessionGame\Content\ObjectPlacement\Blueprints\PBP_ObjectPlacementInventory.uexp";
            StringBuilder builder = new StringBuilder();

            try
            {

                using (var stream = new FileStream(PathToObjectPlacementFile, FileMode.Open, FileAccess.Read))
                {
                    while (stream.Position < stream.Length)
                    {
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
                        int base10Num = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);

                        builder.Append((char)base10Num);
                    }
                }
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }

            builder.ToString();
        }
    }
}
