using System;
using System.IO;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class EzPzMapSwitcherUnitTest
    {
        [TestMethod]
        public void Test_CopyMapFilesToNYCFolder_ReturnsTrue()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;
            MapListItem testMap = new MapListItem()
            {
                FullPath = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "testmap.umap"),
                MapName = "testmap"
            };

            EzPzMapSwitcher switcher = new EzPzMapSwitcher();

            bool result = switcher.CopyMapFilesToNYCFolder(testMap);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Test_CopyMapFilesToNYCFolder_CopiesCorrectlyToFolder()
        {
            string nycPath = Path.Combine(new string[] { TestPaths.ToSessionTestFolder, "SessionGame", "Content", "Art", "Env", "NYC" });

            SessionPath.ToSession = TestPaths.ToSessionTestFolder;
            MapListItem testMap = new MapListItem()
            {
                FullPath = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "testmap.umap"),
                MapName = "testmap"
            };

            // delete files from nyc folder before doing actual copy
            if (Directory.Exists(nycPath))
            {
                Directory.Delete(nycPath, true);
                Directory.CreateDirectory(nycPath);
            }


            EzPzMapSwitcher switcher = new EzPzMapSwitcher();
            bool result = switcher.CopyMapFilesToNYCFolder(testMap);



            Assert.IsTrue(File.Exists(Path.Combine(nycPath, "testmap.umap")));
            Assert.IsTrue(File.Exists(Path.Combine(nycPath, "testmap.uexp")));
            Assert.IsTrue(File.Exists(Path.Combine(nycPath, "testmap_BuiltData.uexp")));
            Assert.IsTrue(File.Exists(Path.Combine(nycPath, "testmap_BuiltData.uasset")));
            Assert.IsTrue(File.Exists(Path.Combine(nycPath, "testmap_BuiltData.ubulk")));
        }
    }
}
