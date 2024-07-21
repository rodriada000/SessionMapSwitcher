using SessionMapSwitcherCore.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.Utils
{
    /// <summary>
    /// (DEPRECATED AS OF v2.6.3)
    /// Was used to handle unpacking the game (old method of modding)
    /// </summary>
    public class UnpackUtils
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
                PathToSession = PathToSession.TrimEnd(new char[] { '/', '\\' });
                return Path.Combine(new string[] { PathToSession, "SessionGame", "Content", "Paks" });
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

                try
                {
                    FileUtils.ExtractZipFile(Path.Combine(PathToPakFolder, DownloadedZipFileName), PathToPakFolder);
                }
                catch (Exception e)
                {
                    ProgressChanged($"Failed to unzip file: {e.Message}. Cannot continue.");
                    UnpackCompleted(false);
                    return;
                }

                bool runSuccess = true; ;

                Task waitTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        RunUnrealPakExe(); // this will wait for UnrealPak to finish
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

                    bool didRename = RenamePakFile();

                    if (didRename == false)
                    {
                        ProgressChanged("Unpacking complete, but failed to rename the .pak file.");
                        UnpackCompleted(false);
                    }

                    // validate files were unpacked by checking subset of expected folders
                    List<string> expectedDirs = new List<string>() { Path.Combine("SessionGame", "Config"), Path.Combine("SessionGame", "Content"), Path.Combine("SessionGame", "Content", "Customization") };
                    foreach (string dir in expectedDirs)
                    {
                        if (Directory.Exists(Path.Combine(PathToSession, dir)) == false)
                        {
                            ProgressChanged($"Failed to unpack files correctly. The expected folders were not found ({Path.Combine(PathToSession, dir)}). Cannot continue.");
                            UnpackCompleted(false);
                            return;
                        }
                    }

                    // delete the original backed up map files so new unpacked files are backedup
                    DeleteOriginalMapFileBackup();

                    UnpackCompleted(true);
                });

            });
        }

        public static void DeleteOriginalMapFileBackup()
        {
            if (Directory.Exists(SessionPath.ToOriginalSessionMapFiles))
            {
                Directory.Delete(SessionPath.ToOriginalSessionMapFiles, true);
            }
        }

        internal bool DownloadZipFile()
        {
            ProgressChanged("Downloading .zip file ...");

            try
            {
                DownloadUtils.ProgressChanged += DownloadUtils_ProgressChanged;

                // visit github to get current anon file download link
                ProgressChanged("Downloading .zip file - getting download url from git ...");
                string downloadUrl = DownloadUtils.GetTextResponseFromUrl(GitHubUrl);

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

                var downloadTask = DownloadUtils.DownloadFileToFolderAsync(directLinkToZip, Path.Combine(PathToPakFolder, DownloadedZipFileName), System.Threading.CancellationToken.None);
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

        internal void RunUnrealPakExe()
        {
            ProgressChanged("Starting UnrealPak.exe ...");

            using (Process proc = new Process())
            {
                proc.StartInfo.WorkingDirectory = this.PathToPakFolder;
                proc.StartInfo.FileName = Path.Combine(PathToPakFolder, "UnrealPak.exe");
                proc.StartInfo.Arguments = $"-cryptokeys=\"Crypto.json\" -Extract \"{PathToPakFolder}\" \"..\\..\\..\"";
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
        /// Determines if Session is properly unpacked by checking for specific directories
        /// </summary>
        public static bool IsSessionUnpacked()
        {
            if (Directory.Exists(SessionPath.ToConfig) == false)
            {
                return false;
            }

            List<string> expectedDirectories = new List<string>() { "Animation", "Art", "Audio", "Challenges", "Character", "Cinematics", "Customization", "Data", "FilmerMode", "MainHUB", "Menus", "Mixer", "ObjectPlacement", "Skateboard", "Skeletons", "Transit", "Tutorial", "VideoEditor" };
            foreach (string expectedDir in expectedDirectories)
            {
                if (Directory.Exists(Path.Combine(SessionPath.ToContent, expectedDir)) == false)
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
