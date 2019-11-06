using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherWPF.Classes;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class PathToUnrealEngineUnitTest
    {
        [TestMethod]
        public void Test_GetPathToUnrealEngine()
        {
            string expected = @"C:\Program Files\Epic Games\UE_4.22";

            string actual = RegistryHelper.GetPathToUnrealEngine();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_IsUnrealPakInstalledLocally_Returns_True()
        {
            EzPzPatcher patcher = new EzPzPatcher()
            {
                PathToUnrealEngine = RegistryHelper.GetPathToUnrealEngine()
            };

            Assert.IsTrue(patcher.IsUnrealPakInstalledLocally());
        }
    }
}
