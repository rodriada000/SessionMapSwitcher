using System;
using System.Collections.Generic;
using System.IO;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class MetaDataManagerUnitTest
    {
        [TestMethod]
        public void Test_GetFirstValidMapInFolder_ValidMap_ReturnsMapListItem()
        {
            MapListItem actualResult = MetaDataManager.GetFirstMapInFolder(Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files"), true);

            Assert.AreEqual("testmap", actualResult.MapName);
        }

        [TestMethod]
        public void Test_CreateMapMetaData_Returns_Correct_FilePaths()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;
            string pathToMapImporting = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "cool_valid_map");

            MapMetaData expectedResult = new MapMetaData()
            {
                FilePaths = new List<string>() { Path.Combine(SessionPath.ToContent, "coolmap.uexp"), Path.Combine(SessionPath.ToContent, "coolmap.umap"),
                                                 Path.Combine(SessionPath.ToContent, "coolmap_BuiltData.uexp"), Path.Combine(SessionPath.ToContent, "coolmap_BuiltData.uasset"),
                                                 Path.Combine(SessionPath.ToContent, "coolmap_BuiltData.ubulk")}
            };

            MapMetaData actualResult = MetaDataManager.CreateMapMetaData(pathToMapImporting, true);

            actualResult.FilePaths.TrueForAll(s => expectedResult.FilePaths.Contains(s));
            expectedResult.FilePaths.TrueForAll(s => actualResult.FilePaths.Contains(s));
        }

        [TestMethod]
        public void Test_CreateMapMetaData_In_SubFolder_Returns_Correct_FilePaths()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;
            string pathToMapImporting = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "some_folder");

            MapMetaData expectedResult = new MapMetaData()
            {
                FilePaths = new List<string>() { Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap.uexp"), 
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap.umap"),
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap_BuiltData.uexp"), 
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap_BuiltData.uasset"),
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap_BuiltData.ubulk")}
            };

            MapMetaData actualResult = MetaDataManager.CreateMapMetaData(pathToMapImporting, true);

            actualResult.FilePaths.TrueForAll(s => expectedResult.FilePaths.Contains(s));
            expectedResult.FilePaths.TrueForAll(s => actualResult.FilePaths.Contains(s));
        }

        [TestMethod]
        public void Test_SaveMapMetaData_Saves_File()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;

            MapMetaData testMetaData = new MapMetaData()
            {
                FilePaths = new List<string>() { "test1", "test2" },
                CustomName = "Test Custom Name",
                MapName = "MapName",
                IsHiddenByUser = false,
                OriginalImportPath = "",
                MapFileDirectory = "Path\\To\\Content\\MapName_Folder"
            };

            MetaDataManager.SaveMapMetaData(testMetaData);

            string pathToExpectedFile = Path.Combine(SessionPath.FullPathToMetaFolder, "MapName_Folder_MapName_meta.json");

            Assert.IsTrue(File.Exists(pathToExpectedFile));
        }

        [TestMethod]
        public void Test_SaveMapMetaData_Saves_Correct_Json()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;

            MapMetaData testMetaData = new MapMetaData()
            {
                FilePaths = new List<string>() { "test1", "test2" },
                CustomName = "Test Custom Name",
                MapName = "MapName",
                IsHiddenByUser = false,
                OriginalImportPath = "",
                MapFileDirectory = "Path\\To\\Content\\MapName_Folder"
            };

            MetaDataManager.SaveMapMetaData(testMetaData);

            string pathToSavedFile = Path.Combine(SessionPath.FullPathToMetaFolder, testMetaData.GetJsonFileName());

            Assert.AreEqual(Newtonsoft.Json.JsonConvert.SerializeObject(testMetaData), File.ReadAllText(pathToSavedFile));
        }
    }
}
