using System;
using System.Diagnostics;
using System.IO;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SessionModManagerCore.Classes
{

    /// <summary>
    /// Used to check if a new version of Session Mod Manager is available to download
    /// and handles the app updating process.
    /// </summary>
    public static class VersionChecker
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static ReleaseInfo LatestRelease { get; set; }
        public static Version CurrentVersion { get; set; }

        public static IProgress<double> ExtractProgress { get; set; }

        public static bool? IsNewVersionAvailable = null;
        public static string PathToLatestZip
        {
            get
            {
                return Path.Combine(SessionPath.ToApplicationRoot, "releases", "latest_smm.zip");
            }
        }

        /// <summary>
        /// If <see cref="IsNewVersionAvailable"/> is null then makes request to release_info.json url to check if there is an update available and sets <see cref="IsNewVersionAvailable"/>.
        /// </summary>
        /// <returns> Returns true if an update is available. </returns>
        public async static Task<bool> IsUpdateAvailable()
        {
            try
            {
                if (IsNewVersionAvailable.HasValue == false)
                {
                    string filepath = Path.Combine(SessionPath.ToApplicationRoot, "release_info.json");
                    string url = AppSettingsUtil.GetAppSetting(SettingKey.VersionCheckUrl);
                    await DownloadUtils.DownloadFileToFolderAsync(url, Path.Combine(SessionPath.ToApplicationRoot, filepath), default);

                    if (!File.Exists(filepath))
                    {
                        Logger.Warn("release_info.json file not found after downloading");
                        return false;
                    }

                    LatestRelease = JsonConvert.DeserializeObject<ReleaseInfo>(File.ReadAllText(filepath));
                    IsNewVersionAvailable = LatestRelease.TypedVersion > CurrentVersion;
                }

                return IsNewVersionAvailable.Value;
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to check for updates");
                return false;
            }
        }

        /// <summary>
        /// downloads latest release and start SMMUpdater which will exit the application to run a seperate console app that copies the latest files.
        /// </summary>
        public async static Task<BoolWithMessage> UpdateApplication()
        {
            if (!await IsUpdateAvailable())
            {
                return BoolWithMessage.True();
            }

            try
            {
                var prepareResult = await PrepareUpdate();

                if (!prepareResult.Result)
                {
                    return prepareResult; // failed to prepare update for some reason
                }

                FileInfo downloadedFile = new FileInfo(PathToLatestZip);
                if (File.Exists(PathToLatestZip))
                {
                    BeginSMMUpdater(downloadedFile.Directory.FullName);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to launch update process");
                return BoolWithMessage.False($"An error occurred while trying to update: {e.Message}");
            }

            return BoolWithMessage.True();
        }

        /// <summary>
        /// Download and extract the latest release zip file
        /// </summary>
        /// <returns></returns>
        public static async Task<BoolWithMessage> PrepareUpdate()
        {
            if (!await IsUpdateAvailable())
            {
                return BoolWithMessage.True();
            }

            try
            {
                FileInfo downloadedFile = new FileInfo(PathToLatestZip);
                string downloadUrl = OperatingSystem.IsLinux() ? LatestRelease.LinuxRelease : LatestRelease.WindowsRelease;

                if (!downloadedFile.Directory.Exists)
                {
                    Directory.CreateDirectory(downloadedFile.Directory.FullName);
                }

                await DownloadUtils.DownloadFileToFolderAsync(downloadUrl, PathToLatestZip, default);

                if (File.Exists(PathToLatestZip))
                {
                    // verify checksum
                    string hash = FileUtils.CalculateMD5(PathToLatestZip);
                    string expectedHash = OperatingSystem.IsWindows() ? LatestRelease.WindowsFileHash : LatestRelease.LinuxFileHash;

                    if (!hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return BoolWithMessage.False($"Aborting update. The downloaded file checksum does not match.\nexpected checksum: {expectedHash}\nactual checksum: {hash}");
                    }

                    // extract files to here
                    FileUtils.ExtractCompressedFile(PathToLatestZip, downloadedFile.Directory.FullName, ExtractProgress);

                    // copy SMMUpdater at this point because it can't copy itself when it's running
                    string smmUpdaterDir = Path.Combine(downloadedFile.Directory.FullName, "Session Mod Manager", "releases");
                    if (Directory.Exists(smmUpdaterDir))
                    {
                        foreach (var file in Directory.GetFiles(smmUpdaterDir))
                        {
                            var fileInfo = new FileInfo(file);
                            File.Copy(file, Path.Combine(SessionPath.ToApplicationRoot, "releases", fileInfo.Name), overwrite: true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to prepare update");
                return BoolWithMessage.False($"An error occurred while trying to prepare update: {e.Message}");
            }

            return BoolWithMessage.True();
        }
        
        /// <summary>
        /// Start SMMUpdater process
        /// </summary>
        /// <param name="workingDir"></param>
        /// <param name="noLaunch"></param>
        public static void BeginSMMUpdater(string workingDir = null, bool noLaunch = false)
        {
            if (string.IsNullOrWhiteSpace(workingDir))
            {
                FileInfo downloadedFile = new FileInfo(PathToLatestZip);
                workingDir = downloadedFile.Directory.FullName;
            }

            if (File.Exists(PathToLatestZip))
            {
                // verify checksum
                string hash = FileUtils.CalculateMD5(PathToLatestZip);
                string expectedHash = OperatingSystem.IsWindows() ? LatestRelease.WindowsFileHash : LatestRelease.LinuxFileHash;

                if (!hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Warn($"Aborting update. The downloaded file checksum does not match.\nexpected checksum: {expectedHash}\nactual checksum: {hash}");
                    return;
                }
            }

            string exeName = OperatingSystem.IsWindows() ? Path.Combine(workingDir, "SMMUpdater.exe") : Path.Combine(workingDir, "SMMUpdater");
            ProcessStartInfo procInfo = new ProcessStartInfo() { WorkingDirectory = workingDir, FileName = exeName, UseShellExecute = false };

            if (noLaunch)
            {
                procInfo.ArgumentList.Add("-nolaunch");
            }

            Process process = Process.Start(procInfo);
        }
    }
}
