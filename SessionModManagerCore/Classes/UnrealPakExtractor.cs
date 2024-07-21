using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.Classes
{
    /// <summary>
    /// Class to provide methods for downloading/running UnrealPak.exe to extract game files
    /// </summary>
    public class UnrealPakExtractor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Name of zip file downloaded with unrealpak.exe files
        /// </summary>
        private const string DownloadedZipFileName = "SessionUnpack.zip";

        /// <summary>
        /// Github link to .txt file that contains the latest download link to the files required for unpacking
        /// </summary>
        private const string UnpackGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/direct_unpackDownloadLink.txt";


        private const string CryptoJsonGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/direct_cryptojsonDownloadLink.txt";


        public delegate void PostMessageDelegate(string message);
        public delegate void ExtractCompleteDelegate(bool wasSuccessful);

        public event PostMessageDelegate ProgressChanged;

        public event ExtractCompleteDelegate ExtractCompleted;

        public bool IsRunningAsAdmin = false;

        public string PathToUnrealEngine { get; set; }

        public string PathToSession;


        public string PathToDownloadedZip
        {
            get => Path.Combine(SessionPath.ToPaks, DownloadedZipFileName);
        }


        /// <summary>
        /// Handles downloading crypto.json,  UnrealPak.exe (if not installed locally) and runs UnrealPak.exe to extract the required game file to modify object count
        /// </summary>
        internal void StartUnrealPakExtraction(string pathToSession)
        {
            this.PathToSession = pathToSession;
            bool didUnrealPakDownload = false;

            Logger.Info("Starting UnrealPak Extraction ...");

            // download the zip file in the background
            Task t = Task.Factory.StartNew(() =>
            {
                //
                // Download required files
                //

                // download the unrealpak files if the user does not have them locally
                didUnrealPakDownload = true;

                if (IsUnrealPakInstalledLocally() == false)
                {
                    didUnrealPakDownload = DownloadUnrealPackZip();
                }
                else if (IsUnrealPakInstalledLocally() && File.Exists(SessionPath.ToCryptoJsonFile) == false)
                {
                    // download crypto.json file
                    didUnrealPakDownload = DownloadCryptoJsonFile();
                }

            });

            t.ContinueWith((task) =>
            {
                if (task.IsFaulted)
                {
                    Logger.Error(task.Exception, "patch task faulted");
                    ExtractCompleted(false);
                    return;
                }

                if (!didUnrealPakDownload)
                {
                    Logger.Warn("Failed to download files, cannot continue");
                    ExtractCompleted(false);
                    return;
                }

                //
                // Extract/Copy Required Files
                //

                if (IsUnrealPakInstalledLocally())
                {
                    ProgressChanged("Copying UnrealPak files ...");
                    BoolWithMessage isUnrealPakCopied = CopyUnrealPakToPakFolder();

                    if (isUnrealPakCopied.Result == false)
                    {
                        ProgressChanged($"Failed to copy UnrealPak: {isUnrealPakCopied.Message}. Cannot continue.");
                        ExtractCompleted(false);
                        return;
                    }
                }
                else
                {
                    ProgressChanged("Extracting UnrealPak .zip files ...");

                    try
                    {
                        FileUtils.ExtractZipFile(PathToDownloadedZip, SessionPath.ToPaks);
                    }
                    catch(Exception e)
                    {
                        ProgressChanged($"Failed to unzip file: {e.Message}. Cannot continue.");
                        ExtractCompleted(false);
                        return;

                    }
                }

                //
                // Run UnrealPak .exe to extract object dropper file
                //

                bool runSuccess = true;

                Task waitTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        ExtractGameFilesFromPak(); // this will wait for UnrealPak to finish
                    }
                    catch (Exception e)
                    {
                        ProgressChanged($"Failed to run UnrealPak.exe: {e.Message}. Cannot continue");
                        runSuccess = false;
                    }
                });

                waitTask.ContinueWith((antecedent) =>
                {
                    if (runSuccess == false)
                    {
                        ExtractCompleted(false);
                        return;
                    }

                    DeleteDownloadedFilesInPakFolder();
                    ExtractCompleted(true);
                });
            });
        }

        private bool DownloadCryptoJsonFile()
        {
            ProgressChanged("Downloading crypto.json file ...");
            Logger.Info("downloading crypto.json ...");


            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged; ;

                // visit github to get current direct download link
                ProgressChanged("Downloading crypto.json file - getting download url from git ...");
                string directLinkToZip = DownloadUtils.GetTextResponseFromUrl(CryptoJsonGitHubUrl);

                directLinkToZip = directLinkToZip.TrimEnd(new char[] { ' ', '\n' });

                // download to Paks folder
                ProgressChanged("Downloading crypto.json file -  downloading actual file ...");
                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(directLinkToZip, SessionPath.ToCryptoJsonFile, System.Threading.CancellationToken.None);
                downloadTask.Wait();

                Logger.Info("... download complete");
            }
            catch (AggregateException e)
            {
                Logger.Error(e);
                ProgressChanged($"Failed to download crypto.json: {e.InnerExceptions[0].Message}. Cannot continue.");
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ProgressChanged($"Failed to download crypto.json: {e.Message}. Cannot continue.");
                return false;
            }
            finally
            {
                DownloadUtils.ProgressChanged -= DownloadUtils_ProgressChanged;
            }

            return true;
        }

        /// <summary>
        /// Uses UnrealPak.exe to extract the files: PBP_ObjectPlacementInventory.uexp and DefaultGame.ini
        /// </summary>
        private void ExtractGameFilesFromPak()
        {
            ProgressChanged("Starting UnrealPak.exe ...");
            Logger.Info("Extracting game files with UnrealPak.exe ...");

            List<string> filesToExtract = new List<string>() { "SessionGame/Content/ObjectPlacement/Blueprints/PBP_ObjectPlacementManager.uexp" };

            try
            {
                foreach (string file in filesToExtract)
                {
                    using (Process proc = new Process())
                    {
                        ProgressChanged($"Extracting file: {file} ...");

                        proc.StartInfo.WorkingDirectory = SessionPath.ToPaks;
                        proc.StartInfo.FileName = Path.Combine(SessionPath.ToPaks, "UnrealPak.exe");
                        proc.StartInfo.Arguments = $"-cryptokeys=\"crypto.json\" -Extract \"{SessionPath.ToPakFile}\" \"..\\..\\..\" -Filter=\"{file}\"";
                        proc.StartInfo.CreateNoWindow = false;

                        if (IsRunningAsAdmin)
                        {
                            proc.StartInfo.Verb = "runas";
                        }

                        proc.Start();
                        proc.WaitForExit();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }


        }

        /// <summary>
        /// Deletes all downloaded files in Paks folder except for 'SessionGame-WindowsNoEditor' files
        /// and the crypto.json file
        /// </summary>
        private void DeleteDownloadedFilesInPakFolder()
        {
            foreach (string filePath in Directory.GetFiles(SessionPath.ToPaks))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;

                if (fileInfo.Extension != ".pak" && fileInfo.Extension != ".sig" && fileName != "crypto.json")
                {
                    File.Delete(filePath);
                }
            }
        }

        /// <summary>
        /// Download SessionUnpack .zip to Paks folder
        /// </summary>
        /// <returns></returns>
        internal bool DownloadUnrealPackZip()
        {
            ProgressChanged("Downloading UnrealPak .zip file ...");
            Logger.Info("UnrealPak.zip downloading");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged; ;

                // visit github to get current direct download link
                ProgressChanged("Downloading UnrealPak .zip file - getting download url from git ...");
                string directLinkToZip = DownloadUtils.GetTextResponseFromUrl(UnpackGitHubUrl);

                directLinkToZip = directLinkToZip.TrimEnd(new char[] { ' ', '\n' });

                // download to Paks folder
                ProgressChanged("Downloading UnrealPak .zip file -  downloading actual file ...");
                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(directLinkToZip, Path.Combine(SessionPath.ToPaks, DownloadedZipFileName), System.Threading.CancellationToken.None);
                downloadTask.Wait();

                Logger.Info("... download complete");
            }
            catch (AggregateException e)
            {
                Logger.Error(e);
                ProgressChanged($"Failed to download .zip file: {e.InnerExceptions[0].Message}. Cannot continue.");
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ProgressChanged($"Failed to download .zip file: {e.Message}. Cannot continue.");
                return false;
            }
            finally
            {
                DownloadUtils.ProgressChanged -= DownloadUtils_ProgressChanged;
            }

            return true;
        }

        private void DownloadUtils_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            ProgressChanged($"Downloading .zip file -  {(double)totalBytesDownloaded / 1000000:0.00} / {(double)totalFileSize / 1000000:0.00} MB | {progressPercentage:0.00}% Complete");
        }


        public BoolWithMessage CopyUnrealPakToPakFolder()
        {
            if (IsUnrealPakInstalledLocally() == false)
            {
                return BoolWithMessage.False("Unreal Engine not installed locally.");
            }

            try
            {
                Logger.Info($"Copying files from {PathToUnrealEngine}");
                string pathToUnrealPak = Path.Combine(new string[] { PathToUnrealEngine, "Engine", "Binaries", "Win64" });

                foreach (string file in Directory.GetFiles(pathToUnrealPak))
                {
                    if (file.Contains("UnrealPak"))
                    {
                        FileInfo info = new FileInfo(file);
                        string targetPath = Path.Combine(SessionPath.ToPaks, info.Name);

                        File.Copy(file, targetPath, overwrite: true);
                    }
                }

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return BoolWithMessage.False($"Failed to copy unrealpak files: {e.Message}");
            }
        }

        public bool IsUnrealPakInstalledLocally()
        {
            if (String.IsNullOrEmpty(PathToUnrealEngine))
            {
                return false;
            }

            return File.Exists(Path.Combine(new string[] { PathToUnrealEngine, "Engine", "Binaries", "Win64", "UnrealPak.exe" }));
        }


    }
}
