public class FileUtils
{

    internal static bool CopyDirectoryRecursively(string sourceDirName, string destDirName, List<string> filesToExclude, List<string> foldersToExclude, bool doContainsSearch, IProgress<double> progress = null)
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
            Console.WriteLine(e.Message);
            return false;
        }

        return true;
    }

    internal static bool CopyDirectoryRecursively(string sourceDirName, string destDirName, IProgress<double> progress = null)
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
            Console.WriteLine(e.Message);
            return false;
        }

        return true;
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

}

