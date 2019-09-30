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
        private string GitHubUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/url_updates/docs/batFileDownloadUrl.txt";

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

                bool isExtracted = ExtractZipFile();

                if (isExtracted == false)
                {
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
                            ProgressChanged("Failed to unpack files correctly. Cannot continue.");
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
                string downloadUrl = GetDownloadUrlFromGit();

                // visit anon file to get direct file download link from html page
                string directLinkToZip = GetDirectDownloadLinkFromAnonPage(downloadUrl);

                if (directLinkToZip == "")
                {
                    return false;
                }

                // download to Paks folder
                DownloadZipFileToPaksFolder(directLinkToZip);
            }
            catch (Exception e)
            {
                ProgressChanged($"Failed to download .zip file: {e.Message}. Cannot continue.");
                return false;
            }

            return true;
        }

        private string GetDownloadUrlFromGit()
        {
            ProgressChanged("Downloading .zip file - getting download url from git ...");
            HttpClient client = new HttpClient();

            Task<HttpResponseMessage> task = client.GetAsync(GitHubUrl);
            task.Wait();

            HttpResponseMessage response = task.Result;
            // Check that response was successful or throw exception
            response.EnsureSuccessStatusCode();

            var readTask = response.Content.ReadAsStringAsync();
            readTask.Wait();

            return readTask.Result;
        }

        private string GetDirectDownloadLinkFromAnonPage(string anonFileUrl)
        {
            ProgressChanged("Downloading .zip file - getting direct download link ...");
            HttpClient client = new HttpClient();

            Task<HttpResponseMessage> requestTask = client.GetAsync(anonFileUrl);
            requestTask.Wait();

            HttpResponseMessage response = requestTask.Result;
            // Check that response was successful or throw exception
            response.EnsureSuccessStatusCode();

            var readTask = response.Content.ReadAsStringAsync();
            readTask.Wait();

            string html = readTask.Result;
            string downloadUrl = FindDirectDownloadUrlInHtml(html);

            return downloadUrl;
        }

        private void DownloadZipFileToPaksFolder(string downloadUrl)
        {
            ProgressChanged("Downloading .zip file -  downloading actual file ...");
            HttpClient client = new HttpClient();

            Task<HttpResponseMessage> requestTask = client.GetAsync(downloadUrl);
            requestTask.Wait();

            // Get HTTP response from completed task.
            HttpResponseMessage response = requestTask.Result;
            // Check that response was successful or throw exception
            response.EnsureSuccessStatusCode();

            using (FileStream w = File.OpenWrite($"{PathToPakFolder}\\{DownloadedZipFileName}"))
            {
                response.Content.CopyToAsync(w).Wait();
            }
        }

        private string FindDirectDownloadUrlInHtml(string html)
        {
            ProgressChanged("Downloading .zip file -  scraping direct download url ...");

            Regex regex = new Regex("<a type=\"button\" id=\"download-url\"[.\\s]*.*\\s*href.*\">");
            Match match = regex.Match(html);

            if (match.Success)
            {
                string downloadUrl = match.Value.TrimEnd(new char[] { '\"', '>', '<' });

                string hrefStr = "href=\"";
                int index = downloadUrl.IndexOf(hrefStr);

                downloadUrl = downloadUrl.Substring(index + hrefStr.Length);

                return downloadUrl;
            }
            else
            {
                ProgressChanged("Could not get direct download link from page. Cannot continue");
                return "";
            }
        }

        internal bool ExtractZipFile()
        {
            ProgressChanged("Extracting .zip file ...");

            try
            {
                
                ZipFile.ExtractToDirectory($"{PathToPakFolder}\\{DownloadedZipFileName}", PathToPakFolder);
            }
            catch (Exception e)
            {
                ProgressChanged($"Failed to unzip file: {e.Message}. Cannot continue.");
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
            ProgressChanged("Copying unpacked files to Session game directory, this may take a few minutes. You should see files being copied to the Content folder that opens ...");
            Process.Start($"{PathToSession}\\SessionGame\\Content");
            FileUtils.MoveDirectoryRecursively($"{PathToPakFolder}\\out", PathToSession, true);
        }
    }
}
