using SessionMapSwitcher.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
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

        public bool SkipUnrealPakStep = false;

        public string PathToSession;

        public string PathToPakFolder
        {
            get
            {
                if (PathToSession.EndsWith("\\"))
                {
                    PathToSession = PathToSession.TrimEnd('\\');
                }

                return $"{PathToSession}\\SessionGame\\Content\\Paks";
            }
        }

        public string PathToDownloadedZip
        {
            get => $"{PathToPakFolder}\\{DownloadedZipFileName}";
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
        private const string UnpackGitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/unpackDownloadLink.txt";


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
                if (IsEzPzExeDownloaded() == false && SkipEzPzPatchStep == false)
                {
                    didEzPzDownload = DownloadEzPzModZip();
                }
                else
                {
                    didEzPzDownload = true;
                }

                if (IsUnpackZipDownloaded() == false && SkipUnrealPakStep == false)
                {
                    didUnrealPakDownload = DownloadUnrealPackZip();
                }
                else
                {
                    didUnrealPakDownload = true;
                }
            });

            t.ContinueWith((task) =>
            {
                if (!didUnrealPakDownload ||  !didEzPzDownload)
                {
                    PatchCompleted(false);
                    return;
                }


                ProgressChanged("Extracting .zip files ...");

                BoolWithMessage isUnrealPakExtracted = BoolWithMessage.True();
                if (SkipUnrealPakStep == false)
                {
                    isUnrealPakExtracted = FileUtils.ExtractZipFile(PathToDownloadedZip, PathToPakFolder);
                }



                BoolWithMessage isEzPzExtracted = BoolWithMessage.True();
                if (IsEzPzExeDownloaded() == false && SkipEzPzPatchStep == false)
                {
                    isEzPzExtracted = FileUtils.ExtractZipFile($"{PathToPakFolder}\\{DownloadedPatchFileName}", PathToPakFolder);
                }

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

                if (isEzPzExtracted.Result == false)
                {
                    ProgressChanged($"Failed to unzip file: {isEzPzExtracted.Message}. Cannot continue.");
                    PatchCompleted(false);
                    return;
                }

                bool runSuccess = true;

                Task waitTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (SkipUnrealPakStep == false)
                        {
                            ExtractGameFilesFromPak(); // this will wait for UnrealPak to finish
                        }

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

            using (Process proc = new Process())
            {
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.WorkingDirectory = @"C:\Windows\System32";
                proc.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
                proc.StartInfo.Arguments = $"/C \"\"{this.PathToPakFolder}\\{EzPzExeName}\" \"{this.PathToSession}\"\"";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                proc.WaitForExit();
            }
        }

        private void ExtractGameFilesFromPak()
        {
            ProgressChanged("Starting UnrealPak.exe ...");

            List<string> filesToExtract = new List<string>() { "SessionGame/Content/ObjectPlacement/Blueprints/PBP_ObjectPlacementInventory.uexp", "SessionGame/Config/DefaultGame.ini" };

            foreach (string file in filesToExtract)
            {
                using (Process proc = new Process())
                {
                    ProgressChanged($"Extracting file: {file} ...");

                    proc.StartInfo.WorkingDirectory = this.PathToPakFolder;
                    proc.StartInfo.FileName = $"{this.PathToPakFolder}\\UnrealPak.exe";
                    proc.StartInfo.Arguments = $"-cryptokeys=\"Crypto.json\" -Extract \"{SessionPath.ToPakFile}\" \"..\\..\\..\" -Filter=\"{file}\"";
                    proc.StartInfo.CreateNoWindow = false;
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
            foreach (string fileName in Directory.GetFiles(PathToPakFolder))
            {
                if (!fileName.Contains("SessionGame-WindowsNoEditor") && !fileName.Contains(EzPzExeName))
                {
                    File.Delete(fileName);
                }
            }
        }

        private bool IsEzPzExeDownloaded()
        {
            return File.Exists($"{PathToPakFolder}\\{EzPzExeName}");
        }

        private bool IsUnpackZipDownloaded()
        {
            return File.Exists(PathToDownloadedZip);
        }

        internal bool DownloadEzPzModZip()
        {
            ProgressChanged("Downloading Session EzPz Mod .zip file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged; ;

                // visit github to get current anon file download link
                ProgressChanged("Downloading Session EzPz Mod .zip file - getting download url from git ...");
                string downloadUrl = DownloadUtils.GetTxtDocumentFromGitHubRepo(EzPzGitHubUrl);

                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(downloadUrl, $"{PathToPakFolder}\\{DownloadedPatchFileName}", System.Threading.CancellationToken.None);
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

        internal bool DownloadUnrealPackZip()
        {
            ProgressChanged("Downloading UnrealPak .zip file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged; ;

                // visit github to get current anon file download link
                ProgressChanged("Downloading UnrealPak .zip file - getting download url from git ...");
                string downloadUrl = DownloadUtils.GetTxtDocumentFromGitHubRepo(UnpackGitHubUrl);

                // visit anon file to get direct file download link from html page
                ProgressChanged("Downloading UnrealPak .zip file -  scraping direct download link download page ...");
                string directLinkToZip = DownloadUtils.GetDirectDownloadLinkFromAnonPage(downloadUrl);

                if (directLinkToZip == "")
                {
                    ProgressChanged("Failed to get download link from html page. Cannot continue.");
                    return false;
                }

                // download to Paks folder
                ProgressChanged("Downloading UnrealPak .zip file -  downloading actual file ...");
                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(directLinkToZip, $"{PathToPakFolder}\\{DownloadedZipFileName}", System.Threading.CancellationToken.None);
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
            return File.Exists($"{SessionPath.ToConfig}\\UserEngine.ini");
        }
    }
}
