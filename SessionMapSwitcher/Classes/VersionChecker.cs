using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Classes
{
    class VersionChecker
    {
        private const string LatestReleaseUrl = "https://github.com/rodriada000/SessionMapSwitcher/releases/latest";

        public static bool IsNewVersionAvailable()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    Task<HttpResponseMessage> task = client.GetAsync(LatestReleaseUrl);
                    task.Wait();

                    HttpResponseMessage response = task.Result;
                    // Check that response was successful or throw exception
                    response.EnsureSuccessStatusCode();

                    Uri actualReleaseUri = response.RequestMessage.RequestUri;

                    Version latestVersion = GetVersionFromUrl(actualReleaseUri.AbsoluteUri);

                    return (App.GetAppVersion().CompareTo(latestVersion) < 0);
                }
            }
            catch (Exception)
            {
                // silently fail if fails to update... just skip the update check
                return false;
            }
        }

        public static void OpenLatestReleaseInBrowser()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = LatestReleaseUrl
            };
            Process.Start(startInfo);
        }

        private static Version GetVersionFromUrl(string url)
        {
            int index = url.LastIndexOf("/");

            string versionFromUrl = url.Substring(index + 1).TrimStart('v');

            // version from url is three digits (1.2.0) so append another 0 to make it 1.2.0.0
            Version version = Version.Parse($"{versionFromUrl}.0");

            return version;
        }
    }
}
