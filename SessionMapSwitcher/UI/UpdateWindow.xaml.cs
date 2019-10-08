using SessionMapSwitcher.Classes;
using SessionMapSwitcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SessionMapSwitcher.UI
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
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
                htmlVersionNotes = VersionChecker.ScrapeLatestVersionNotesFromGitHub();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, scheduler);

            scraperTask.ContinueWith((antecedent) =>
            {
                browser.NavigateToString(htmlVersionNotes);
                ViewModel.BrowserVisibility = Visibility.Visible;
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
            VersionChecker.AppUpdater.ReportProgress += AppUpdater_ReportProgress; ;

            Task updateTask = Task.Factory.StartNew(() =>
            {
                VersionChecker.UpdateApplication();
            });

            updateTask.ContinueWith((updateAntecedent) =>
            {
                VersionChecker.AppUpdater.ReportProgress -= AppUpdater_ReportProgress;
            });
        }

        private void AppUpdater_ReportProgress(NAppUpdate.Framework.Common.UpdateProgressInfo currentStatus)
        {
            ViewModel.HeaderMessage = $"Updating app: {currentStatus.Message} | {currentStatus.Percentage}%";
        }
    }
}
