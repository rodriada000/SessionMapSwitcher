using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapSwitcherUnitTests.Helpers
{
    public static class TestPaths
    {
        public static string ToSessionTestFolder
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, "TestFiles", "SessionFolders");
            }
        }

        public static string ToTestFilesFolder
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, "TestFiles");
            }
        }

        public static string ToMockTextureFilesFolder
        {
            get
            {
                return Path.Combine(ToTestFilesFolder, "Mock_Texture_Files");
            }
        }
    }
}
