using System;
using System.IO;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class UnpackUtilsUnitTest
    {
        [TestMethod]
        public void Test_DeleteOriginalMapFileBackup_Folder_Exists_DeletesCorrectly()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;
            Directory.CreateDirectory(SessionPath.ToOriginalSessionMapFiles); // ensure direct exists before testing delete

            
            UnpackUtils.DeleteOriginalMapFileBackup();

            Assert.IsFalse(Directory.Exists(SessionPath.ToOriginalSessionMapFiles));
        }
    }
}
