using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Utils
{
    class FileUtils
    {
        internal static void CopyDirectoryRecursively(string sourceDirName, string destDirName, bool copySubDirs)
        {
            CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, copySubDirs, moveFiles: false);
        }

        internal static void MoveDirectoryRecursively(string sourceDirName, string destDirName, bool copySubDirs)
        {
            CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, copySubDirs, moveFiles: true);
        }

        private static void CopyOrMoveDirectoryRecursively(string sourceDirName, string destDirName, bool includSubeFolders, bool moveFiles)
        {
            // reference: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories

            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);

                if (!File.Exists(temppath))
                {
                    if (moveFiles)
                    {
                        file.MoveTo(temppath);
                    }
                    else
                    {
                        file.CopyTo(temppath, true);
                    }
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (includSubeFolders)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();

                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyOrMoveDirectoryRecursively(subdir.FullName, temppath, includSubeFolders, moveFiles);
                }
            }
        }
    }
}
