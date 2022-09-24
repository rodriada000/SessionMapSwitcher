using NLog;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace SessionMapSwitcher.UI
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private UpdateViewModel ViewModel { get; set; }

        public UpdateWindow()
        {
            InitializeComponent();

            ViewModel = new UpdateViewModel();
            this.DataContext = ViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetVersionNotesInBackground();
        }

        private void GetVersionNotesInBackground()
        {
            string htmlVersionNotes = "";

            TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();


            Task scraperTask = Task.Factory.StartNew(() =>
            {
                htmlVersionNotes = ScrapeLatestVersionNotesFromGitHub();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, scheduler);

            scraperTask.ContinueWith((antecedent) =>
            {
                if (antecedent.IsFaulted)
                {
                    Logger.Error(antecedent.Exception.GetBaseException());
                }

                browser.NavigateToString(htmlVersionNotes);
                ViewModel.IsBrowserVisible = true;
                browser.Visibility = ViewModel.IsBrowserVisible ? Visibility.Visible : Visibility.Hidden;
            }, scheduler);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.HeaderMessage = "Updating app ...";
            BoolWithMessage updateResult = null;

            Task updateTask = Task.Factory.StartNew(() =>
            {
                updateResult = VersionChecker.UpdateApplication();
            });

            updateTask.ContinueWith((updateAntecedent) =>
            {
                if (updateResult?.Result == false)
                {
                    System.Windows.MessageBox.Show(updateResult.Message, "Error Updating!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        #region Methods related to getting version notes

        /// <summary>
        /// Scrapes the latest release git hub page for version notes by looking for the div tag
        /// with the class "markdown-body"
        /// </summary>
        /// <returns> Scraped html from Github if found </returns>
        public static string ScrapeLatestVersionNotesFromGitHub()
        {
            string pageHtml = DownloadUtils.GetTextResponseFromUrl(UpdateViewModel.LatestReleaseUrl);
            HtmlDocument doc = GetHtmlDocument(pageHtml);

            string fullHtml = "";
            bool foundHeader = false;
            bool foundbody = false;

            // append css style to the scraped html so it the document does not load with default Arial font
            fullHtml += "<style type=\"text/css\"> * { font-family: -apple-system,BlinkMacSystemFont,Segoe UI,Helvetica,Arial,sans-serif,Apple Color Emoji,Segoe UI Emoji; background: #221E1F; color: white } a { pointer-events: none; cursor: default; color: #799BAC } </style>";

            // loop over html elements and find the 'release-header' div and 'markdown-body' div
            foreach (HtmlElement element in doc.Body.All)
            {
                if (element.GetAttribute("className").Contains("Box-body") && element.Children.Count >= 3)
                {
                    DisableHyperLinksInHtml(element.Children[1]);
                    DisableHyperLinksInHtml(element.Children[2]);

                    fullHtml += element.Children[1].InnerHtml;
                    fullHtml += element.Children[2].InnerHtml;

                    foundHeader = true;
                }

                if (foundHeader)
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
