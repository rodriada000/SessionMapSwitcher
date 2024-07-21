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
            string expected = @"C:\Program Files\Epic Games\UE_4.24";

            string actual = RegistryHelper.GetPathToUnrealEngine();

            Assert.AreEqual(expected, actual);
        }
    }
}
