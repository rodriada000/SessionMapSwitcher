using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.ViewModels;
using System.Threading.Tasks;
using System.Threading;
using SessionModManagerCore.Classes;
using MsBox.Avalonia;

namespace SessionModManagerAvalonia;

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

    private void GetVersionNotesInBackground()
    {
        string htmlVersionNotes = "";

        TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();


        Task scraperTask = Task.Factory.StartNew(() =>
        {
            htmlVersionNotes = ScrapeLatestVersionNotesFromGitHub();
            htmlPanel.Text = htmlVersionNotes;
        }, CancellationToken.None, TaskCreationOptions.LongRunning, scheduler);

        scraperTask.ContinueWith((antecedent) =>
        {
            if (antecedent.IsFaulted)
            {
                Logger.Error(antecedent.Exception.GetBaseException());
            }

            //browser.NavigateToString(htmlVersionNotes);
            ViewModel.IsBrowserVisible = true;
            //browser.Visibility = ViewModel.IsBrowserVisible ? Visibility.Visible : Visibility.Hidden;
        }, scheduler);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        //this.DialogResult = false;
        this.Close(false);
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.HeaderMessage = "Updating app ...";
        BoolWithMessage updateResult = null;

        Task updateTask = Task.Factory.StartNew(() =>
        {
            updateResult = VersionChecker.UpdateApplication();
        });

        await updateTask.ContinueWith(async (updateAntecedent) =>
        {
            if (updateResult?.Result == false)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error Updating!", updateResult.Message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                var result = await box.ShowAsync();
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
        return DownloadUtils.GetTextResponseFromUrl(UpdateViewModel.LatestReleaseUrl);
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        GetVersionNotesInBackground();
    }

    ///// <summary>
    ///// sets the 'onclick' attribute to 'return false' so the hyperlink is disabled
    ///// </summary>
    ///// <param name="child"></param>
    //private static void DisableHyperLinksInHtml(HtmlElement child)
    //{
    //    foreach (HtmlElement link in child.GetElementsByTagName("a"))
    //    {
    //        link.SetAttribute("onClick", "return false;");
    //    }
    //}

    ///// <summary>
    ///// Uses a WebBrowser control to get an HtmlDocument from a html string
    ///// </summary>
    //public static HtmlDocument GetHtmlDocument(string html)
    //{
    //    using (WebBrowser browser = new WebBrowser())
    //    {
    //        browser.ScriptErrorsSuppressed = true;
    //        browser.DocumentText = html;
    //        browser.Document.OpenNew(true);
    //        browser.Document.Write(html);
    //        browser.Refresh();

    //        return browser.Document;
    //    }
    //}

    #endregion
}