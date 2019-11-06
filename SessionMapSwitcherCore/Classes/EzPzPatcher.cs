using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.Classes
{
    /// <summary>
    /// Class to provide methods for patching the game with dga's EzPz Mod.
    /// </summary>
    public class EzPzPatcher
    {
        public delegate void PostMessageDelegate(string message);
        public delegate void PatchCompleteDelegate(bool wasSuccessful);

        public event PostMessageDelegate ProgressChanged;

        public event PatchCompleteDelegate PatchCompleted;

        public bool SkipEzPzPatchStep = false;

        public bool SkipUnrealPakStep = true;

        public bool IsRunningAsAdmin = false;

        public string PathToUnrealEngine { get; set; }

        public string PathToSession;


        public string PathToDownloadedZip
        {
            get => Path.Combine(SessionPath.ToPaks, DownloadedZipFileName);
        }

        /// <summary>
        /// Name of zip file downloaded with unrealpak.exe files
        /// </summary>
        private const string DownloadedZipFileName = "SessionUnpack.zip";

        /// <summary>
        /// Name of zip file with EzPz patcher exe
        /// </summary>
        private const string DownloadedPatchFileName = "SessionEzPzMod.zip";

        private const string EzPzExeName = "SessionEzPzMod.exe";


        /// <summary>
        /// Github link to .txt file that contains the latest download link to the files required for patching
        /// </summary>
        private const string EzPzGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/ezpzDownloadLink.txt";


        /// <summary>
        /// Github link to .txt file that contains the latest download link to the files required for unpacking
        /// </summary>
        private const string UnpackGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/direct_unpackDownloadLink.txt";


        private const string CryptoJsonGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/direct_cryptojsonDownloadLink.txt";

        /// <summary>
        /// Handles the entire patching process
        /// ... download zip files
        /// ... extract zip files
        /// ... run UnrealPak.exe and EzPz
        /// </summary>
        internal void StartPatchingAsync(string pathToSession)
        {
            this.PathToSession = pathToSession;
            bool didEzPzDownload = false;
            bool didUnrealPakDownload = false;

            // download the zip file in the background
            Task t = Task.Factory.StartNew(() =>
            {
                //
                // Download required files
                //

                // download the EzPz .zip if the .exe does not exist already and we are NOT skipping the patching step
                if (IsEzPzExeDownloaded() == false && SkipEzPzPatchStep == false)
                {
                    didEzPzDownload = DownloadEzPzModZip();
                }
                else
                {
                    didEzPzDownload = true;
                }

                // download the unrealpak files if the user does not have them locally and the .zip is not downloaded
                didUnrealPakDownload = true;

                if (SkipUnrealPakStep == false)
                {
                    if (IsUnpackZipDownloaded() == false && IsUnrealPakInstalledLocally() == false)
                    {
                        didUnrealPakDownload = DownloadUnrealPackZip();
                    }
                    else if (IsUnrealPakInstalledLocally() && File.Exists(SessionPath.ToCryptoJsonFile) == false)
                    {
                        // download crypto.json file
                        didUnrealPakDownload = DownloadCryptoJsonFile();
                    }
                }

            });

            t.ContinueWith((task) =>
            {
                if (!didUnrealPakDownload || !didEzPzDownload)
                {
                    PatchCompleted(false);
                    return;
                }

                //
                // Extract/Copy Required Files
                //

                if (SkipUnrealPakStep == false)
                {
                    if (IsUnrealPakInstalledLocally())
                    {
                        ProgressChanged("Copying UnrealPak files ...");
                        BoolWithMessage isUnrealPakCopied = CopyUnrealPakToPakFolder();

                        if (isUnrealPakCopied.Result == false)
                        {
                            ProgressChanged($"Failed to copy UnrealPak: {isUnrealPakCopied.Message}. Cannot continue.");
                            PatchCompleted(false);
                            return;
                        }
                    }
                    else
                    {
                        ProgressChanged("Extracting UnrealPak .zip files ...");
                        BoolWithMessage isUnrealPakExtracted = FileUtils.ExtractZipFile(PathToDownloadedZip, SessionPath.ToPaks);

                        if (isUnrealPakExtracted.Result == false)
                        {
                            if (IsUnpackZipDownloaded())
                            {
                                File.Delete(PathToDownloadedZip);
                            }

                            ProgressChanged($"Failed to unzip file: {isUnrealPakExtracted.Message}. Cannot continue.");
                            PatchCompleted(false);
                            return;
                        }
                    }
                }


                if (SkipEzPzPatchStep == false)
                {
                    if (IsEzPzExeDownloaded() == false)
                    {
                        ProgressChanged("Extracting EzPz .zip files ...");
                        BoolWithMessage isEzPzExtracted = FileUtils.ExtractZipFile(Path.Combine(SessionPath.ToPaks, DownloadedPatchFileName), SessionPath.ToPaks);

                        if (isEzPzExtracted.Result == false)
                        {
                            ProgressChanged($"Failed to unzip file: {isEzPzExtracted.Message}. Cannot continue.");
                            PatchCompleted(false);
                            return;
                        }
                    }
                }

                //
                // Run EzPz Mod .exe and UnrealPak .exe to extract some file
                //

                bool runSuccess = true;

                Task waitTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (SkipEzPzPatchStep == false)
                        {
                            LaunchEzPzMod();
                        }

                        if (SkipUnrealPakStep == false)
                        {
                            ExtractGameFilesFromPak(); // this will wait for UnrealPak to finish
                        }
                    }
                    catch (Exception e)
                    {
                        ProgressChanged($"Failed to run UnrealPak.exe or EzPz Mod: {e.Message}. Cannot continue");
                        runSuccess = false;
                    }
                });

                waitTask.ContinueWith((antecedent) =>
                {
                    if (runSuccess == false)
                    {
                        PatchCompleted(false);
                        return;
                    }

                    DeleteDownloadedFilesInPakFolder();
                    PatchCompleted(true);
                });
            });
        }

        private bool DownloadCryptoJsonFile()
        {
            ProgressChanged("Downloading crypto.json file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged; ;

                // visit github to get current direct download link
                ProgressChanged("Downloading crypto.json file - getting download url from git ...");
                string directLinkToZip = DownloadUtils.GetTxtDocumentFromGitHubRepo(CryptoJsonGitHubUrl);

                directLinkToZip = directLinkToZip.TrimEnd(new char[] { ' ', '\n' });

                // download to Paks folder
                ProgressChanged("Downloading crypto.json file -  downloading actual file ...");
                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(directLinkToZip, SessionPath.ToCryptoJsonFile, System.Threading.CancellationToken.None);
                downloadTask.Wait();
            }
            catch (AggregateException e)
            {
                ProgressChanged($"Failed to download crypto.json: {e.InnerExceptions[0].Message}. Cannot continue.");
                return false;
            }
            catch (Exception e)
            {
                ProgressChanged($"Failed to download crypto.json: {e.Message}. Cannot continue.");
                return false;
            }
            finally
            {
                DownloadUtils.ProgressChanged -= DownloadUtils_ProgressChanged;
            }

            return true;
        }

        private void LaunchEzPzMod()
        {
            ProgressChanged("Starting Session EzPz Mod. Click 'Patch' when the window opens then close it after completion ...");

            using (Process proc = new Process())
            {
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.WorkingDirectory = @"C:\Windows\System32";
                proc.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
                proc.StartInfo.Arguments = $"/C \"\"{SessionPath.ToPaks}\\{EzPzExeName}\" \"{this.PathToSession}\"\"";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                if (IsRunningAsAdmin)
                {
                    proc.StartInfo.Verb = "runas";
                }

                proc.Start();
                proc.WaitForExit();
            }
        }

        /// <summary>
        /// Uses UnrealPak.exe to extract the files: PBP_ObjectPlacementInventory.uexp & DefaultGame.ini
        /// </summary>
        private void ExtractGameFilesFromPak()
        {
            ProgressChanged("Starting UnrealPak.exe ...");

            List<string> filesToExtract = new List<string>() { "SessionGame/Content/ObjectPlacement/Blueprints/PBP_ObjectPlacementInventory.uexp" };

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

        /// <summary>
        /// Deletes all downloaded files in Paks folder except for 'SessionGame-WindowsNoEditor' files
        /// and the EzPzMod.exe 
        /// </summary>
        private void DeleteDownloadedFilesInPakFolder()
        {
            foreach (string filePath in Directory.GetFiles(SessionPath.ToPaks))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;

                if (!fileName.Contains("SessionGame-WindowsNoEditor") && fileName != EzPzExeName && fileName != "crypto.json")
                {
                    File.Delete(filePath);
                }
            }
        }

        /// <summary>
        /// Returns true if EzPz .exe is in Paks folder
        /// </summary>
        /// <returns></returns>
        private bool IsEzPzExeDownloaded()
        {
            return File.Exists(Path.Combine(SessionPath.ToPaks, EzPzExeName));
        }

        /// <summary>
        /// Returns true if SessionUnpack.zip is in Paks folder
        /// </summary>
        /// <returns></returns>
        private bool IsUnpackZipDownloaded()
        {
            return File.Exists(PathToDownloadedZip);
        }

        /// <summary>
        /// Download EzPz .zip to Paks folder
        /// </summary>
        /// <returns></returns>
        internal bool DownloadEzPzModZip()
        {
            ProgressChanged("Downloading Session EzPz Mod .zip file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged; ;

                // visit github to get current anon file download link
                ProgressChanged("Downloading Session EzPz Mod .zip file - getting download url from git ...");
                string downloadUrl = DownloadUtils.GetTxtDocumentFromGitHubRepo(EzPzGitHubUrl);

                downloadUrl = downloadUrl.TrimEnd(new char[] { ' ', '\n' });

                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(downloadUrl, Path.Combine(SessionPath.ToPaks, DownloadedPatchFileName), System.Threading.CancellationToken.None);
                downloadTask.Wait();
            }
            catch (AggregateException e)
            {
                ProgressChanged($"Failed to download .zip file: {e.InnerExceptions[0].Message}. Cannot continue.");
                return false;
            }
            catch (Exception e)
            {
                ProgressChanged($"Failed to download .zip file: {e.Message}. Cannot continue.");
                return false;
            }
            finally
            {
                DownloadUtils.ProgressChanged -= DownloadUtils_ProgressChanged;
            }

            return true;
        }

        /// <summary>
        /// Download SessionUnpack .zip to Paks folder
        /// </summary>
        /// <returns></returns>
        internal bool DownloadUnrealPackZip()
        {
            ProgressChanged("Downloading UnrealPak .zip file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged; ;

                // visit github to get current direct download link
                ProgressChanged("Downloading UnrealPak .zip file - getting download url from git ...");
                string directLinkToZip = DownloadUtils.GetTxtDocumentFromGitHubRepo(UnpackGitHubUrl);

                directLinkToZip = directLinkToZip.TrimEnd(new char[] { ' ', '\n' });

                // download to Paks folder
                ProgressChanged("Downloading UnrealPak .zip file -  downloading actual file ...");
                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(directLinkToZip, Path.Combine(SessionPath.ToPaks, DownloadedZipFileName), System.Threading.CancellationToken.None);
                downloadTask.Wait();
            }
            catch (AggregateException e)
            {
                ProgressChanged($"Failed to download .zip file: {e.InnerExceptions[0].Message}. Cannot continue.");
                return false;
            }
            catch (Exception e)
            {
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

        /// <summary>
        /// Checks if EzPz has been ran by looking for the UserEngine.ini file
        /// </summary>
        public static bool IsGamePatched()
        {
            return File.Exists(SessionPath.ToUserEngineIniFile);
        }


        public BoolWithMessage CopyUnrealPakToPakFolder()
        {
            if (IsUnrealPakInstalledLocally() == false)
            {
                return BoolWithMessage.False("Unreal Engine not installed locally.");
            }

            try
            {
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
