using System;
using System.IO;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class MapListItemUnitTest
    {
        [TestMethod]
        public void Test_DirectoryPath_ReturnsCorrectPath()
        {
            MapListItem testItem = new MapListItem()
            {
                FullPath = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "testmap.umap"),
                MapName = "testmap"
            };

            string expectedPath = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files");

            Assert.AreEqual(expectedPath, testItem.DirectoryPath);
        }

        [TestMethod]
        public void Test_HasGameMode_ValidMap_ReturnsTrue()
        {
            string pathToValidMap = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "testmap.umap");

            Assert.IsTrue(MapListItem.HasGameMode(pathToValidMap));
        }

        [TestMethod]
        public void Test_HasGameMode_InvalidMap_ReturnsFalse()
        {
            string pathToInvalidMap = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "testmap_invalid.umap");

            Assert.IsFalse(MapListItem.HasGameMode(pathToInvalidMap));
        }

        [TestMethod]
        public void Test_Validate_MissingFile_ValidationHint_Set()
        {
            MapListItem testItem = new MapListItem()
            {
                FullPath = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "notAMap.umap"),
                MapName = "notAMap"
            };

            testItem.Validate();

            string expectedHint = "(file missing)";

            Assert.AreEqual(expectedHint, testItem.ValidationHint);
        }
    }
}
