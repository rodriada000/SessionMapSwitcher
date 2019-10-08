using SessionMapSwitcher.Classes;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Utils
{
    public class FileUtils
    {
        internal static void CopyDirectoryRecursively(string sourceDirName, string destDirName, List<string> filesToExclude, List<string> foldersToInclude)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = false,
                CopySubFolders = true,
                ExcludeFiles = filesToExclude,
                ExcludeFolders = foldersToInclude
            };

            CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
        }

        internal static void CopyDirectoryRecursively(string sourceDirName, string destDirName)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = false,
                CopySubFolders = true,
            };

            CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
        }

        internal static void MoveDirectoryRecursively(string sourceDirName, string destDirName, List<string> filesToExclude, List<string> foldersToInclude)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = true,
                CopySubFolders = true,
                ExcludeFiles = filesToExclude,
                ExcludeFolders = foldersToInclude
            };

            CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
        }

        internal static void MoveDirectoryRecursively(string sourceDirName, string destDirName)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = true,
                CopySubFolders = true,
            };

            CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
        }

        private static void CopyOrMoveDirectoryRecursively(string sourceDirName, string destDirName, CopySettings settings)
        {
            if (settings.ExcludeFiles == null)
            {
                settings.ExcludeFiles = new List<string>();
            }

            if (settings.ExcludeFolders == null)
            {
                settings.ExcludeFolders = new List<string>();
            }

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
                if (settings.ExcludeFiles.Contains(file.Name))
                {
                    continue; // skip file as it is excluded
                }

                string temppath = Path.Combine(destDirName, file.Name);

                if (settings.IsMovingFiles)
                {
                    if (File.Exists(temppath))
                    {
                        File.Delete(temppath); // delete existing file before moving new file
                    }
                    file.MoveTo(temppath);
                }
                else
                {
                    file.CopyTo(temppath, true);
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (settings.CopySubFolders)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();

                foreach (DirectoryInfo subdir in dirs)
                {
                    if (settings.ExcludeFolders.Contains(subdir.Name))
                    {
                        continue; // skip folder as it is excluded
                    }

                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    CopyOrMoveDirectoryRecursively(subdir.FullName, tempPath, settings);
                }
            }
        }


        /// <summary>
        /// Extract a zip file to a given path. Returns true on success.
        /// </summary>
        public static BoolWithMessage ExtractZipFile(string pathToZip, string extractPath)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(pathToZip))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fullFileName = Path.Combine(extractPath, entry.FullName);
                        string entryPath = Path.GetDirectoryName(fullFileName);

                        if (Directory.Exists(entryPath) == false)
                        {
                            Directory.CreateDirectory(entryPath);
                        }

                        bool isFileToExtract = (Path.GetFileName(fullFileName) != "");

                        if (isFileToExtract)
                        {
                            entry.ExtractToFile(Path.GetFullPath(fullFileName), overwrite: true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return new BoolWithMessage(false, e.Message);
            }

            return new BoolWithMessage(true);
        }

        public static BoolWithMessage ExtractRarFile(string pathToRar, string extractPath)
        {
            try
            {
                using (RarArchive archive = RarArchive.Open(pathToRar))
                {
                    foreach (RarArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
            catch (Exception e)
            {
                return new BoolWithMessage(false, e.Message);
            }

            return new BoolWithMessage(true);
        }

        public static BoolWithMessage ExtractCompressedFile(string pathToFile, string extractPath)
        {
            if (pathToFile.EndsWith(".zip"))
            {
                return ExtractZipFile(pathToFile, extractPath);
            }
            else if (pathToFile.EndsWith(".rar"))
            {
                return ExtractRarFile(pathToFile, extractPath);
            }

            return new BoolWithMessage(false, "Unsupported file type.");
        }
    }

}
