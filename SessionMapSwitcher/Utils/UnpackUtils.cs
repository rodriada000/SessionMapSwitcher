using SessionMapSwitcher.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Utils
{
    class UnpackUtils
    {
        public delegate void SendMessageDelegate(string message);
        public delegate void UnpackCompleteDelegate(bool wasSuccessful);

        public event SendMessageDelegate ProgressChanged;

        public event UnpackCompleteDelegate UnpackCompleted;

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

        /// <summary>
        /// Name of zip file downloaded
        /// </summary>
        private const string DownloadedZipFileName = "SessionUnpack.zip";

        /// <summary>
        /// Github link to .txt file that contains the latest download link to the files required for unpacking
        /// </summary>
        private const string GitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/batFileDownloadUrl_v2.txt";

        /// <summary>
        /// Handles the entire unpacking process
        /// ... download zip file
        /// ... extract zip file
        /// ... run UnrealPak.exe
        /// ... copy unpacked files to Session folder
        /// </summary>
        internal void StartUnpackingAsync(string pathToSession)
        {
            this.PathToSession = pathToSession;
            bool didDownload = false;

            // download the zip file in the background
            Task t = Task.Factory.StartNew(() =>
            {
                didDownload = DownloadZipFile();
            });

            t.ContinueWith((task) =>
            {
                if (didDownload == false)
                {
                    UnpackCompleted(false);
                    return;
                }

                ProgressChanged("Extracting .zip file ...");
                BoolWithMessage isExtracted = FileUtils.ExtractZipFile($"{PathToPakFolder}\\{DownloadedZipFileName}", PathToPakFolder);

                if (isExtracted.Result == false)
                {
                    ProgressChanged($"Failed to unzip file: {isExtracted.Message}. Cannot continue.");
                    UnpackCompleted(false);
                    return;
                }

                bool runSuccess = true; ;

                Task waitTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        RunUnrealPakBatFile(); // this will wait for UnrealPak to finish
                    }
                    catch (Exception e)
                    {
                        ProgressChanged($"Failed to run UnrealPak.exe : {e.Message}. Cannot continue");
                        runSuccess = false;
                    }
                });

                waitTask.ContinueWith((unrealTask) =>
                {
                    if (runSuccess == false)
                    {
                        UnpackCompleted(false);
                        return;
                    }

                    // validate files were unpacked by checking subset of expected folders
                    List<string> expectedDirs = new List<string>() { "\\SessionGame\\Config", "\\SessionGame\\Content", "\\SessionGame\\Content\\Customization" };
                    foreach (string dir in expectedDirs)
                    {
                        if (Directory.Exists($"{PathToSession}{dir}") == false)
                        {
                            ProgressChanged($"Failed to unpack files correctly. The expected folders were not found ({PathToSession}{dir}). Cannot continue.");
                            UnpackCompleted(false);
                            return;
                        }
                    }

                    UnpackCompleted(true);
                });

            });
        }

        internal bool DownloadZipFile()
        {
            ProgressChanged("Downloading .zip file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged;

                // visit github to get current anon file download link
                ProgressChanged("Downloading .zip file - getting download url from git ...");
                string downloadUrl = DownloadUtils.GetTxtDocumentFromGitHubRepo(GitHubUrl);

                // visit anon file to get direct file download link from html page
                ProgressChanged("Downloading .zip file -  scraping direct download link download page ...");
                string directLinkToZip = DownloadUtils.GetDirectDownloadLinkFromAnonPage(downloadUrl);

                if (directLinkToZip == "")
                {
                    ProgressChanged("Failed to get download link from html page. Cannot continue.");
                    return false;
                }

                // download to Paks folder
                ProgressChanged("Downloading .zip file -  downloading actual file ...");

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

        internal void RunUnrealPakBatFile()
        {
            ProgressChanged("Starting UnrealPak.exe bat file ...");

            using (Process proc = new Process())
            {
                proc.StartInfo.WorkingDirectory = this.PathToPakFolder;
                proc.StartInfo.FileName = GetBatFileName(this.PathToPakFolder);
                proc.StartInfo.CreateNoWindow = false;
                proc.Start();


                ProgressChanged("Waiting for UnrealPak.exe to finish ...");
                proc.WaitForExit();
            }

            System.Threading.Thread.Sleep(500);

            // delete the UnrealPak.exe files and .bat file since done using
            foreach (string fileName in Directory.GetFiles(PathToPakFolder))
            {
                if (fileName.Contains("SessionGame-WindowsNoEditor") == false)
                {
                    File.Delete(fileName);
                }
            }
        }

        /// <summary>
        /// Finds .bat file from given directory and returns name of .bat file
        /// </summary>
        private string GetBatFileName(string directory)
        {
            foreach (string fileName in Directory.GetFiles(directory))
            {
                if (fileName.EndsWith(".bat"))
                {
                    int slashIndex = fileName.LastIndexOf('\\');
                    string batFile = fileName.Substring(slashIndex + 1);
                    return batFile;
                }
            }

            return "";
        }

        internal bool CopyUnpackedFilesToSession()
        {
            string outFolderPath = $"{PathToPakFolder}\\out";

            ProgressChanged("Copying unpacked files to Session game directory, this may take a few minutes. You should see files being copied to the Content folder that opens ...");
            Process.Start($"{PathToSession}\\SessionGame\\Content");

            try
            {
                FileUtils.MoveDirectoryRecursively(outFolderPath, PathToSession);

                System.Threading.Thread.Sleep(500);

                // delete out file since empty now
                if (Directory.Exists(outFolderPath))
                {
                    Directory.Delete(outFolderPath, true);
                }
                return true;
            }
            catch(Exception e)
            {
                ProgressChanged($"Failed to copy files to Session game directory: {e.Message}. Unpacking failed.");
                return false;
            }
        }

        /// <summary>
        /// Determines if Session is properly unpacked by checking for specific directories
        /// </summary>
        public static bool IsSessionUnpacked()
        {
            if (Directory.Exists(SessionPath.ToConfig) == false)
            {
                return false;
            }

            List<string> expectedDirectories = new List<string>() { "Animation", "Art", "Character", "Customization", "ObjectPlacement", "MainHUB", "Skateboard", "VideoEditor" };
            foreach (string expectedDir in expectedDirectories)
            {
                if (Directory.Exists($"{SessionPath.ToContent}\\{expectedDir}") == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if the file extension for SessionGame-WindowsNoEditor.pak was changed to .bak
        /// </summary>
        public static bool IsSessionPakFileRenamed()
        {
            return (File.Exists(SessionPath.ToPakFile) == false);
        }

        /// <summary>
        /// Renames the file SessionGame-WindowsNoEditor.pak to SessionGame-WindowsNoEditor.bak
        /// </summary>
        /// <returns> true if file was renamed; false otherwise. </returns>
        public static bool RenamePakFile()
        {
            if (IsSessionPakFileRenamed())
            {
                return true; // already renamed nothing to do
            }

            try
            {
                string bakFileName = SessionPath.ToPakFile.Replace(".pak", ".bak");

                // check if for some reason the .pak file and .bak file both exist; delete the .bak file so that moving .pak -> .bak file does not fail
                if (File.Exists(SessionPath.ToPakFile) && File.Exists(bakFileName))
                {
                    File.Delete(bakFileName);
                }

                File.Move(SessionPath.ToPakFile, bakFileName);
                System.Threading.Thread.Sleep(750); // wait a second after renaming the file ensure it is updated (due to race conditions where the next process starts too soon before realzing the file name changed) 
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
