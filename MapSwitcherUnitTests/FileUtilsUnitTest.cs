﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class FileUtilsUnitTest
    {
        [TestMethod]
        public void Test_ExtractRarFile_ExtractsCorrectly()
        {
            string pathToRar = Path.Combine(TestPaths.ToTestFilesFolder, "testRarFile.rar");
            string extractPath = Path.Combine(TestPaths.ToTestFilesFolder, "TestDir");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            //BoolWithMessage result = FileUtils.ExtractRarFile(pathToRar, extractPath);

            //Assert.IsTrue(result.Result);

            bool extractedFileExists = File.Exists(Path.Combine(extractPath, "testFile.txt"));
            Assert.IsTrue(extractedFileExists);

            bool mapDirExists = Directory.Exists(Path.Combine(extractPath, "testFolder"));
            Assert.IsTrue(mapDirExists);


            extractedFileExists = File.Exists(Path.Combine(extractPath, "testFolder", "testFile.txt"));
            Assert.IsTrue(extractedFileExists);
        }

        [TestMethod]
        public void Test_ExtractZipFile_ExtractsCorrectly()
        {
            string pathToZip = Path.Combine(TestPaths.ToTestFilesFolder, "testZipFile.zip");
            string extractPath = Path.Combine(TestPaths.ToTestFilesFolder, "TestDir");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            //BoolWithMessage result = FileUtils.ExtractZipFile(pathToZip, extractPath);

            //Assert.IsTrue(result.Result);

            bool extractedFileExists = File.Exists(Path.Combine(extractPath, "testFile.txt"));
            Assert.IsTrue(extractedFileExists);

            bool mapDirExists = Directory.Exists(Path.Combine(extractPath, "testFolder"));
            Assert.IsTrue(mapDirExists);


            extractedFileExists = File.Exists(Path.Combine(extractPath, "testFolder", "testFile.txt"));
            Assert.IsTrue(extractedFileExists);
        }

        [TestMethod]
        public void Test_ExtractZipFile_InvalidFile_ReturnsFalse()
        {
            string pathToZip = Path.Combine(TestPaths.ToTestFilesFolder, "NotAZip.zip");
            string extractPath = Path.Combine(TestPaths.ToTestFilesFolder, "TestDir");



            //BoolWithMessage result = FileUtils.ExtractZipFile(pathToZip, extractPath);

            //Assert.IsFalse(result.Result);
        }

        [TestMethod]
        public void Test_GetAllFilesInDirectory_ReturnsCorrectResult()
        {
            string sourceDir = Path.Combine(TestPaths.ToTestFilesFolder, "folder_with_files");

            List<string> expectedResult = new List<string>() { Path.Combine(sourceDir, "empty.txt"), Path.Combine(sourceDir, "subfolder", "empty.txt") };

            List<string> actualResult = FileUtils.GetAllFilesInDirectory(sourceDir);

            Assert.IsTrue(actualResult.TrueForAll(s => expectedResult.Contains(s)));
        }

        [TestMethod]
        public void Test_GetAllFilesInDirectory_InvalidDir_ReturnsEmptyList()
        {
            List<string> actualResult = FileUtils.GetAllFilesInDirectory("not a dir");

            Assert.AreEqual(0, actualResult.Count);
        }
    }
}
