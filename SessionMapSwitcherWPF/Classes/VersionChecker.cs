using NAppUpdate.Framework;
using NAppUpdate.Framework.Tasks;
using SessionMapSwitcherCore.Utils;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SessionMapSwitcherWPF.Classes
{
    class VersionChecker
    {
        public const string LatestReleaseUrl = "https://github.com/rodriada000/SessionMapSwitcher/releases/latest";

        private const string UpdateFeedUrl = "https://raw.githubusercontent.com/rodriada000/SessionMapSwitcher/release_updates/latest_release/updatefeed.xml";

        private const string _nameOfExe = "SessionMapSwitcher.exe";

        /// <summary>
        /// Get the instance of <see cref="UpdateManager.Instance"/>
        /// </summary>
        public static UpdateManager AppUpdater
        {
            get
            {
                return UpdateManager.Instance;
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

        /// <summary>
        /// Makes request to feed.xml url to and check if there is an update available
        /// </summary>
        /// <returns> Returns true if an update is available. </returns>
        public static bool CheckForUpdates()
        {
            try
            {
                AppUpdater.UpdateSource = new NAppUpdate.Framework.Sources.SimpleWebSource(UpdateFeedUrl);
                AppUpdater.CheckForUpdates();

                return HasUpdatesAvailable();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loops over the update tasks and looks for 'SessionMapSwitcher.exe'
        /// </summary>
        /// <returns> true if FileUpdateTask is found with the name 'SessionMapSwitcher.exe' </returns>
        public static bool HasUpdatesAvailable()
        {
            foreach (IUpdateTask task in AppUpdater.Tasks)
            {
                if (task is FileUpdateTask)
                {
                    FileUpdateTask fileTask = (task as FileUpdateTask);
                    if (fileTask.LocalPath == _nameOfExe)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void UpdateApplication()
        {
            if (HasUpdatesAvailable())
            {
                try
                {
                    AppUpdater.PrepareUpdates();
                    AppUpdater.ApplyUpdates(true, true, false);
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show($"An error occurred while trying to update: {e.Message}", "Error Updating!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    AppUpdater.CleanUp();
                }
            }
        }


        #region Methods related to getting version notes

        /// <summary>
        /// Scrapes the latest release git hub page for version notes by looking for the div tag
        /// with the class "markdown-body"
        /// </summary>
        /// <returns> Scraped html from Github if found </returns>
        public static string ScrapeLatestVersionNotesFromGitHub()
        {
            string pageHtml = DownloadUtils.GetTxtDocumentFromGitHubRepo(LatestReleaseUrl);
            HtmlDocument doc = GetHtmlDocument(pageHtml);

            string fullHtml = "";
            bool foundHeader = false;
            bool foundbody = false;

            // append css style to the scraped html so it the document does not load with default Arial font
            fullHtml += "<style type=\"text/css\"> * { font-family: -apple-system,BlinkMacSystemFont,Segoe UI,Helvetica,Arial,sans-serif,Apple Color Emoji,Segoe UI Emoji; background: #CFD8DC } a { pointer-events: none; cursor: default; } </style>";

            // loop over html elements and find the 'release-header' div and 'markdown-body' div
            foreach (HtmlElement element in doc.Body.All)
            {
                if (element.GetAttribute("className").Contains("release-header"))
                {
                    foreach (HtmlElement child in element.Children)
                    {
                        // skip the unordered list that is hidden in header that has commit hash
                        if (child.TagName.Equals("ul", StringComparison.OrdinalIgnoreCase) == false)
                        {
                            DisableHyperLinksInHtml(child);

                            fullHtml += child.InnerHtml;
                            fullHtml += "<br/>";
                        }
                    }
                    foundHeader = true;
                }

                if (element.GetAttribute("className").Contains("markdown-body"))
                {
                    fullHtml += element.InnerHtml;
                    foundbody = true;
                }

                if (foundbody && foundHeader)
                {
                    return fullHtml;
                }
            }

            return "Could not locate version notes";
        }

        /// <summary>
        /// sets the 'onclick' attribute to 'return false' so the hyperlink is disabled
        /// </summary>
        /// <param name="child"></param>
        private static void DisableHyperLinksInHtml(HtmlElement child)
        {
            foreach (HtmlElement link in child.GetElementsByTagName("a"))
            {
                link.SetAttribute("onClick", "return false;");
            }
        }

        /// <summary>
        /// Uses a WebBrowser control to get an HtmlDocument from a html string
        /// </summary>
        public static HtmlDocument GetHtmlDocument(string html)
        {
            using (WebBrowser browser = new WebBrowser())
            {
                browser.ScriptErrorsSuppressed = true;
                browser.DocumentText = html;
                browser.Document.OpenNew(true);
                browser.Document.Write(html);
                browser.Refresh();

                return browser.Document;
            }
        }

        #endregion

    }
}
