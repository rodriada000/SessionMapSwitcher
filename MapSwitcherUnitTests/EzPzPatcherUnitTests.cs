using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcher.Classes;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class EzPzPatcherUnitTests
    {
        [TestMethod]
        public void Test_GetPathToUnrealEngine()
        {
            string expected = @"C:\Program Files\Epic Games\UE_4.22";

            string actual = EzPzPatcher.GetPathToUnrealEngine();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Test_IsUnrealPakInstalledLocally_Returns_True()
        {
            Assert.IsTrue(EzPzPatcher.IsUnrealPakInstalledLocally());
        }
    }
}
