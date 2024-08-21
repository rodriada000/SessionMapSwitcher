using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SessionMapSwitcherCore.Utils
{
    public class FileUtils
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static List<string> CopyDirectoryRecursively(string sourceDirName, string destDirName, List<string> filesToExclude, List<string> foldersToExclude, bool doContainsSearch, IProgress<double> progress = null)
        {
            List<string> filesCopied = new List<string>();

            if (filesToExclude == null)
            {
                filesToExclude = new List<string>();
            }

            if (foldersToExclude == null)
            {
                foldersToExclude = new List<string>();
            }

            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = false,
                CopySubFolders = true,
                ExcludeFiles = filesToExclude,
                ExcludeFolders = foldersToExclude,
                ContainsSearchForFiles = doContainsSearch
            };

            try
            {
                int count = 0, totalCount = 0;
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings, ref totalCount, ref count, filesCopied, progress);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return filesCopied;
        }

        internal static void CopyDirectoryRecursively(string sourceDirName, string destDirName, IProgress<double> progress = null)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = false,
                CopySubFolders = true,
                ContainsSearchForFiles = false
            };

            try
            {
                int count = 0, totalCount = 0;
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings, fileCount: ref totalCount, currentCount: ref count, copiedFiles: null, progress: progress);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal static void MoveDirectoryRecursively(string sourceDirName, string destDirName, List<string> filesToExclude, List<string> foldersToExclude, bool doContainsSearch, IProgress<double> progress = null)
        {
            if (filesToExclude == null)
            {
                filesToExclude = new List<string>();
            }

            if (foldersToExclude == null)
            {
                foldersToExclude = new List<string>();
            }

            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = true,
                CopySubFolders = true,
                ExcludeFiles = filesToExclude,
                ExcludeFolders = foldersToExclude,
                ContainsSearchForFiles = doContainsSearch
            };

            try
            {
                int count = 0, totalCount = 0;
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings, fileCount: ref totalCount, currentCount: ref count, copiedFiles: null, progress: progress);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal static void MoveDirectoryRecursively(string sourceDirName, string destDirName, IProgress<double> progress = null)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = true,
                CopySubFolders = true,
                ContainsSearchForFiles = false
            };

            try
            {
                int count = 0, totalCount = 0;
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings, fileCount: ref totalCount, currentCount: ref count, copiedFiles: null, progress: progress);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static void CopyOrMoveDirectoryRecursively(string sourceDirName, string destDirName, CopySettings settings, ref int fileCount, ref int currentCount, List<string> copiedFiles = null, IProgress<double> progress = null)
        {
            if (copiedFiles == null)
            {
                copiedFiles = new List<string>();
            }

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

            // Get the files in the directory and copy them to the new location.
            if (fileCount == 0)
            {
                fileCount = GetAllFilesInDirectory(sourceDirName).Count;
            }

            FileInfo[] files = dir.GetFiles();
            progress?.Report((double)currentCount / (double)fileCount);

            foreach (FileInfo file in files)
            {
                if (settings.ExcludeFile(file))
                {
                    continue; // skip file as it is excluded
                }

                // If the destination directory doesn't exist, create it.
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
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

                currentCount++;
                progress?.Report((double)currentCount / (double)fileCount);
                copiedFiles.Add(temppath);
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
                    CopyOrMoveDirectoryRecursively(subdir.FullName, tempPath, settings, ref fileCount, ref currentCount, copiedFiles, progress);
                }
            }
        }


        /// <summary>
        /// Extract a zip file to a given path. Returns true on success.
        /// </summary>
        public static List<string> ExtractZipFile(string pathToZip, string extractPath, IProgress<double> progress = null)
        {
            List<string> filesExtracted = new List<string>();
            int entryCount = 1;
            int currentCount = 0;

            Logger.Info($"extracting .zip {pathToZip} ...");

            using (ZipArchive archive = OpenRead(pathToZip))
            {
                Logger.Info("... Opened .zip for read");

                entryCount = archive.Entries.Count;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fullFileName = Path.Combine(extractPath, entry.FullName).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    string entryPath = Path.GetDirectoryName(fullFileName);

                    Logger.Info($"... {fullFileName} -> {entryPath}");

                    if (Directory.Exists(entryPath) == false)
                    {
                        Logger.Info($"... creating missing directory {entryPath}");
                        Directory.CreateDirectory(entryPath);
                    }

                    bool isFileToExtract = (Path.GetFileName(fullFileName) != "");

                    Logger.Info($"... {fullFileName}, isFileToExtract: {isFileToExtract}");

                    if (isFileToExtract)
                    {
                        Logger.Info($"...... extracting");
                        using (Stream deflatedStream = entry.Open())
                        {
                            using (FileStream fileStream = File.Create(fullFileName))
                            {
                                deflatedStream.CopyTo(fileStream);
                            }
                        }
                        filesExtracted.Add(fullFileName);
                        Logger.Info($"......... extracted!");
                    }

                    currentCount++;
                    progress?.Report((double)currentCount / (double)entryCount);
                }
            }

            return filesExtracted;
        }

        public static List<string> ExtractRarFile(string pathToRar, string extractPath, IProgress<double> progress = null)
        {
            List<string> filesExtracted = new List<string>();
            Logger.Info($"extracting .rar {pathToRar} ...");
            int entryCount = 1;
            int currentCount = 0;

            using (RarArchive archive = RarArchive.Open(pathToRar))
            {
                Logger.Info("... Opened .rar for read");
                entryCount = archive.Entries.Count;

                foreach (RarArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    Logger.Info($"...... extracting {entry.Key}");

                    entry.WriteToDirectory(extractPath, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });

                    currentCount++;
                    progress?.Report((double)currentCount / (double)entryCount);
                    filesExtracted.Add(Path.Combine(extractPath, entry.Key));
                    Logger.Info($"......... extracted!");
                }
            }

            return filesExtracted;
        }

        public static List<string> ExtractCompressedFile(string pathToFile, string extractPath, IProgress<double> progress = null)
        {
            if (pathToFile.EndsWith(".zip"))
            {
                return ExtractZipFile(pathToFile, extractPath, progress);
            }
            else if (pathToFile.EndsWith(".rar"))
            {
                return ExtractRarFile(pathToFile, extractPath, progress);
            }

            Logger.Warn($"Unsupported file type: {pathToFile}");
            return new List<string>();
        }

        public enum SearchType
        {
            StartsWith,
            Contains,
            EndsWith
        }

        public static bool CompressedFileHasFile(string pathToFile, string searchPattern, SearchType searchType = SearchType.Contains)
        {
            bool hasFile = false;

            if (pathToFile.EndsWith(".zip"))
            {
                using (ZipArchive archive = OpenRead(pathToFile))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (searchType == SearchType.StartsWith && entry.FullName.StartsWith(searchPattern))
                        {
                            hasFile = true;
                            break;
                        }
                        else if (searchType == SearchType.Contains && entry.FullName.Contains(searchPattern))
                        {
                            hasFile = true;
                            break;
                        }
                        else if (searchType == SearchType.EndsWith && entry.FullName.EndsWith(searchPattern))
                        {
                            hasFile = true;
                            break;
                        }
                    }
                }
            }
            else if (pathToFile.EndsWith(".rar"))
            {
                using (RarArchive archive = RarArchive.Open(pathToFile))
                {
                    foreach (RarArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        if (searchType == SearchType.StartsWith && entry.Key.StartsWith(searchPattern))
                        {
                            hasFile = true;
                            break;
                        }
                        else if (searchType == SearchType.Contains && entry.Key.Contains(searchPattern))
                        {
                            hasFile = true;
                            break;
                        }
                        else if (searchType == SearchType.EndsWith && entry.Key.EndsWith(searchPattern))
                        {
                            hasFile = true;
                            break;
                        }
                    }
                }
            }

            return hasFile;
        }


        public static ZipArchive OpenRead(string filename)
        {
            return new ZipArchive(File.OpenRead(filename), ZipArchiveMode.Read);
        }

        /// <summary>
        /// Recursively get all files in a given directory
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static List<string> GetAllFilesInDirectory(string directoryPath)
        {
            List<string> allFiles = new List<string>();

            if (Directory.Exists(directoryPath) == false)
            {
                return allFiles;
            }

            foreach (string file in Directory.GetFiles(directoryPath))
            {
                allFiles.Add(file);
            }

            foreach (string dir in Directory.GetDirectories(directoryPath))
            {
                List<string> subDirFiles = GetAllFilesInDirectory(dir);

                if (subDirFiles.Count > 0)
                {
                    allFiles.AddRange(subDirFiles);
                }
            }

            return allFiles;
        }

        public static BoolWithMessage DeleteFiles(List<string> filesToDelete)
        {
            try
            {
                HashSet<string> possibleFoldersToDelete = new HashSet<string>(); // this will be a list of directories where files were deleted; if these directories are empty then they will also be deleted

                foreach (string file in filesToDelete)
                {
                    if (File.Exists(file))
                    {
                        FileInfo fileInfo = new FileInfo(file);

                        if (possibleFoldersToDelete.Contains(fileInfo.DirectoryName) == false)
                        {
                            possibleFoldersToDelete.Add(fileInfo.DirectoryName);
                        }


                        File.Delete(file);
                    }
                }

                // delete the possible empty directories
                foreach (string folder in possibleFoldersToDelete)
                {
                    // iteratively go up parent folder structure to delete empty folders after files have been deleted
                    string currentDir = folder;

                    if (Directory.Exists(currentDir) && currentDir != SessionPath.ToContent)
                    {
                        List<string> remainingFiles = GetAllFilesInDirectory(currentDir);

                        while (remainingFiles.Count == 0 && currentDir != SessionPath.ToContent)
                        {
                            string dirToDelete = currentDir;

                            DirectoryInfo dirInfo = new DirectoryInfo(currentDir);
                            currentDir = dirInfo.Parent.FullName; // get path to parent directory to check next

                            Directory.Delete(dirToDelete, true);

                            if (currentDir != SessionPath.ToContent)
                            {
                                remainingFiles = GetAllFilesInDirectory(currentDir); // get list of files from parent dir to check next
                            }
                        }
                    }
                }

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to delete files: {e.Message}");
            }

        }

        public static byte[] StringToByteArray(String hex)
        {
            // reference: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa

            if (hex.Length % 2 != 0)
            {
                // pad with '0' for odd length strings like 'A' so it becomes '0A' or '1A4' => '01A4'
                hex = '0' + hex;
            }

            int numChars = hex.Length;
            byte[] bytes = new byte[numChars / 2];
            for (int i = 0; i < numChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static int FindSequenceInArray(List<string> arrayToSearch, List<string> sequence, int startIndex = 0)
        {
            if (startIndex == 0)
            {
                startIndex = arrayToSearch.Count - 1;
            }
            // reference: https://stackoverflow.com/questions/55150204/find-subarray-in-array-in-c-sharp
            // iterate backwards, stop if the rest of the array is shorter than needle (i >= needle.Length)
            for (int i = startIndex; i >= sequence.Count - 1; i--)
            {
                bool found = true;
                // also iterate backwards through needle, stop if elements do not match (!found)
                for (int j = sequence.Count - 1; j >= 0 && found; j--)
                {
                    // compare needle's element with corresponding element of haystack
                    found = arrayToSearch[i - (sequence.Count - 1 - j)] == sequence[j];
                }

                if (found)
                {
                    // result was found, i is now the index of the last found element, so subtract needle's length - 1
                    return i - (sequence.Count - 1);
                }
            }

            // not found, return -1
            return -1;
        }

        public static List<string> StringToHexArray(string str)
        {
            byte[] strBytes = Encoding.Default.GetBytes(str);
            string hexPairs = BitConverter.ToString(strBytes);
            return hexPairs.Split('-').ToList();
        }

        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

    }

}
