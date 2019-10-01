using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        private const string GitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/batFileDownloadUrl.txt";

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
                bool isExtracted = FileUtils.ExtractZipFile($"{PathToPakFolder}\\{DownloadedZipFileName}", PathToPakFolder);

                if (isExtracted == false)
                {
                    ProgressChanged($"Failed to unzip file. Cannot continue.");
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
                    List<string> expectedDirs = new List<string>() { "\\out\\SessionGame\\Config", "\\out\\SessionGame\\Content", "\\out\\SessionGame\\Content\\Customization" };
                    foreach (string dir in expectedDirs)
                    {
                        if (Directory.Exists($"{PathToPakFolder}{dir}") == false)
                        {
                            ProgressChanged($"Failed to unpack files correctly. The expected folders were not found ({PathToPakFolder}{dir}). Cannot continue.");
                            UnpackCompleted(false);
                            return;
                        }
                    }

                    CopyUnpackedFilesToSession();
                    UnpackCompleted(true);
                });

            });
        }

        internal bool DownloadZipFile()
        {
            ProgressChanged("Downloading .zip file ...");

            try
            {
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

            return true;
        }

        internal void RunUnrealPakBatFile()
        {
            ProgressChanged("Starting UnrealPak.exe bat file ...");

            Process proc = new Process();
            proc.StartInfo.WorkingDirectory = this.PathToPakFolder;
            proc.StartInfo.FileName = GetBatFileName(this.PathToPakFolder);
            proc.StartInfo.CreateNoWindow = false;
            proc.Start();


            ProgressChanged("Waiting for UnrealPak.exe to finish ...");
            proc.WaitForExit();

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

        internal void CopyUnpackedFilesToSession()
        {
            string outFolderPath = $"{PathToPakFolder}\\out";

            ProgressChanged("Copying unpacked files to Session game directory, this may take a few minutes. You should see files being copied to the Content folder that opens ...");
            Process.Start($"{PathToSession}\\SessionGame\\Content");
            FileUtils.MoveDirectoryRecursively(outFolderPath, PathToSession, true);

            System.Threading.Thread.Sleep(500);

            // delete out file since empty now
            if (Directory.Exists(outFolderPath))
            {
                Directory.Delete(outFolderPath, true);
            }
        }
    }
}
