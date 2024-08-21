using System;

namespace SessionModManagerCore.Classes
{
    public class ReleaseInfo
    {
        public string Version { get; set; }

        public string WindowsRelease { get; set; }
        public string WindowsFileHash { get; set; }

        public string LinuxRelease { get; set; }
        public string LinuxFileHash { get; set; }

        public string VersionNotes { get; set; }

        public Version TypedVersion
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Version) ? new Version(Version) : null;
            }
        }

    }
}
