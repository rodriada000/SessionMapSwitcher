using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.Classes
{
    /// <summary>
    /// (DEPRECATED AS OF v2.6.3)
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


        /// <summary>
        /// Name of zip file with EzPz patcher exe
        /// </summary>
        private const string DownloadedPatchFileName = "SessionEzPzMod.zip";

        private const string EzPzExeName = "SessionEzPzMod.exe";


        /// <summary>
        /// Github link to .txt file that contains the latest download link to the files required for patching
        /// </summary>
        private const string EzPzGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/ezpzDownloadLink.txt";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

            Logger.Info("Starting EzPz Patch Process ...");

            // download the zip file in the background
            Task t = Task.Factory.StartNew(() =>
            {
                //
                // Download required files
                //

                // download the EzPz .zip if we are NOT skipping the patching step
                if (SkipEzPzPatchStep == false)
                {
                    didEzPzDownload = DownloadEzPzModZip();
                    Logger.Info($"didEzPzDownload: {didEzPzDownload}");
                }
                else
                {
                    Logger.Info($"... skipping ezpz download");
                    didEzPzDownload = true;
                }

            });

            t.ContinueWith((task) =>
            {
                if (task.IsFaulted)
                {
                    Logger.Error(task.Exception, "patch task faulted");
                    PatchCompleted(false);
                    return;
                }

                if (!didEzPzDownload)
                {
                    Logger.Warn("Failed to download files, cannot continue");
                    PatchCompleted(false);
                    return;
                }


                if (SkipEzPzPatchStep == false)
                {
                    string pathToZip = Path.Combine(SessionPath.ToPaks, DownloadedPatchFileName);
                    Logger.Info($"Extracting Ezpz.zip; File exist? {File.Exists(pathToZip)}");

                    if (File.Exists(pathToZip))
                    {
                        ProgressChanged("Extracting EzPz .zip files ...");
                        BoolWithMessage isEzPzExtracted = BoolWithMessage.False("");

                        try
                        {
                            FileUtils.ExtractZipFile(pathToZip, SessionPath.ToPaks);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "failed to extract zip");
                            ProgressChanged($"Failed to unzip file: {e.Message}. Cannot continue.");
                            isEzPzExtracted.Message = e.Message;
                            PatchCompleted(false);
                        }

                        if (isEzPzExtracted.Result == false)
                        {
                            ProgressChanged($"Failed to unzip file: {isEzPzExtracted.Message}. Cannot continue.");
                            PatchCompleted(false);
                            return;
                        }
                    }
                    else
                    {
                        ProgressChanged($".zip file does not exist - {pathToZip}");
                        PatchCompleted(false);
                        return;
                    }
                }

                //
                // Run EzPz Mod .exe and/or UnrealPak .exe to extract object dropper file
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

        private void LaunchEzPzMod()
        {
            ProgressChanged("Starting Session EzPz Mod. Click 'Patch' when the window opens then close it after completion ...");

            try
            {
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
            catch (Exception e)
            {
                Logger.Error(e);
            }

        }

        /// <summary>
        /// Uses UnrealPak.exe to extract the files: PBP_ObjectPlacementInventory.uexp and DefaultGame.ini
        /// </summary>
        private void ExtractGameFilesFromPak()
        {
            ProgressChanged("Starting UnrealPak.exe ...");
            Logger.Info("Extracting game files with UnrealPak.exe ...");

            List<string> filesToExtract = new List<string>() { "SessionGame/Content/ObjectPlacement/Blueprints/PBP_ObjectPlacementInventory.uexp" };

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
        /// Download EzPz .zip to Paks folder
        /// </summary>
        /// <returns></returns>
        internal bool DownloadEzPzModZip()
        {
            ProgressChanged("Downloading Session EzPz Mod .zip file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged;

                // visit github to get current anon file download link
                ProgressChanged("Downloading Session EzPz Mod .zip file - getting download url from git ...");
                Logger.Info("Downloading Session EzPz Mod .zip file ...");

                string downloadUrl = DownloadUtils.GetTextResponseFromUrl(EzPzGitHubUrl);

                downloadUrl = downloadUrl.TrimEnd(new char[] { ' ', '\n' });

                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(downloadUrl, Path.Combine(SessionPath.ToPaks, DownloadedPatchFileName), System.Threading.CancellationToken.None);
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
                Logger.Info("... unsubscribing to event");
                DownloadUtils.ProgressChanged -= DownloadUtils_ProgressChanged;
                Logger.Info("... unsubscribed");

            }

            return true;
        }

        private void DownloadUtils_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            ProgressChanged($"Downloading .zip file -  {(double)totalBytesDownloaded / 1000000:0.00} / {(double)totalFileSize / 1000000:0.00} MB | {progressPercentage:0.00}% Complete");
        }

    }
}
