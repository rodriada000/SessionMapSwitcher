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

        public event SendMessageDelegate ProgressChanged;

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

        private string DownloadedZipFileName = "";

        internal void StartUnpacking(string pathToSession)
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
                    ProgressChanged("Failed to download .zip file. Cannot continue.");
                    return;
                }

                ExtractZipFile();

                RunUnrealPakBatFile(); // this will wait for UnrealPak to finish then kick off the rest of the process
            });
        }

        private void WaitForUnrealPakToFinishInBackground()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                Process[] unrealPakProc;

                do
                {
                    unrealPakProc = Process.GetProcessesByName("UnrealPak");
                    System.Threading.Thread.Sleep(10000);
                } while (unrealPakProc?.Length > 0);

                System.Threading.Thread.Sleep(5000); // wait few seconds to ensure 
            });
        }

        internal bool DownloadZipFile()
        {
            ProgressChanged("Downloading .zip file ...");

            // visit anon file to get direct file download link from html page
            // https://anonfile.com/o6X1l777nd/SessionUnpack_v0.6_rar

            // direct file link example: https://cdn-04.anonfile.com/o6X1l777nd/a9cf134d-1569740585/SessionUnpack_v0.6.rar

            // download to Paks folder

            return true;
        }

        internal void ExtractZipFile()
        {
            ProgressChanged("Extracting .zip file ...");

        }

        internal void RunUnrealPakBatFile()
        {
            ProgressChanged("Starting UnrealPak.exe bat file ...");

            Process proc = new Process();
            proc.StartInfo.WorkingDirectory = this.PathToPakFolder;
            proc.StartInfo.FileName = GetBatFileName(this.PathToPakFolder);
            proc.StartInfo.CreateNoWindow = false;
            proc.Start();

            Task waitTask = Task.Factory.StartNew(() =>
            {
                ProgressChanged("Waiting for UnrealPak.exe to finish ...");
                proc.WaitForExit();
            });

            waitTask.ContinueWith((task) =>
            {
                // validate files were unpacked by checking subset of expected folders
                List<string> expectedDirs = new List<string>() { "\\out\\SessionGame\\Config", "\\out\\SessionGame\\Content", "\\out\\SessionGame\\Content\\Customization" };
                foreach (string dir in expectedDirs)
                {
                    if (Directory.Exists($"{PathToPakFolder}{dir}") == false)
                    {
                        ProgressChanged("Failed to unpack files correctly. Cannot continue.");
                        return;
                    }
                }

                CopyUnpackedFilesToSession();
            });

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
            ProgressChanged("Copying unpacked files to Session game directory ...");

            FileUtils.MoveDirectoryRecursively($"{PathToPakFolder}\\out", PathToSession, true);
        }
    }
}
