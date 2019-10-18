using System.IO;
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
    }
}
