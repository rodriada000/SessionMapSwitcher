using System;
using System.IO;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.ViewModels;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class TextureReplacerUnitTest
    {
        [TestMethod]
        public void Test_GetPathFromTextureFile_ValidFile_Returns_Correct_Path()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;

            FileInfo testFile = new FileInfo(Path.Combine(TestPaths.ToMockTextureFilesFolder, "AMXX_GEN_LB_StraightCut_Pants_A.uasset"));

            string expectedResult = "/Game/Customization/Characters/AMXX/LowerBody/Skelmeshes/AMXX_GEN_LB_StraightCut_Pants_A".Replace('/', Path.DirectorySeparatorChar);

            string actualResult = TextureReplacerViewModel.GetPathFromTextureFile(testFile);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void Test_GetFolderPathToTextureFromFile_ValidFile_Returns_Correct_Path()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;

            FileInfo testFile = new FileInfo(Path.Combine(TestPaths.ToMockTextureFilesFolder, "AMXX_GEN_LB_StraightCut_Pants_A.uasset"));

            string expectedResult = SessionPath.ToContent + "/Customization/Characters/AMXX/LowerBody/Skelmeshes".Replace('/', Path.DirectorySeparatorChar);

            string actualResult = TextureReplacerViewModel.GetFolderPathToTextureFromFile(testFile);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void Test_GetTextureNameFromFile_ValidFile_Returns_Correct_Name()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;

            FileInfo testFile = new FileInfo(Path.Combine(TestPaths.ToMockTextureFilesFolder, "AMXX_GEN_LB_StraightCut_Pants_A.uasset"));

            string expectedResult = "AMXX_GEN_LB_StraightCut_Pants_A";

            string actualResult = TextureReplacerViewModel.GetTextureNameFromFile(testFile);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void Test_GetObjectCountFromSavFile()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;


            GameSettingsManager.GetFileAddressesOfHexString();

        }
    }
}
