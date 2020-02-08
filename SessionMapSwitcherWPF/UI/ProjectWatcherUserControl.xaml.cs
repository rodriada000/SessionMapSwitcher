using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.ViewModels;
using System.Windows.Forms;

namespace SessionMapSwitcher.UI
{
    /// <summary>
    /// Interaction logic for ProjectWatcherUserControl.xaml
    /// </summary>
    public partial class ProjectWatcherUserControl : System.Windows.Controls.UserControl
    {
        public ProjectWatcherViewModel ViewModel { get; set; }

        public ProjectWatcherUserControl()
        {
            InitializeComponent();

            ViewModel = new ProjectWatcherViewModel();
            this.DataContext = ViewModel;
        }

        private void BtnBrowse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BrowseForProject();
        }

        private void BtnWatch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.WatchProject();   
        }

        private void BtnUnwatch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.UnwatchProject();
        }

        internal void BrowseForProject()
        {
            ViewModel.UnwatchProject();

            using (var projectFileBrowser = new OpenFileDialog())
            {
                projectFileBrowser.Filter = "Unreal Engine Projects (*.uproject)|*.uproject";
                projectFileBrowser.Title = "Select .uproject File";
                if (projectFileBrowser.ShowDialog() == DialogResult.OK)
                {
                    ViewModel.PathToProject = projectFileBrowser.FileName;
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.ProjectWatcherPath, ViewModel.PathToProject);
                }
            }

            ViewModel.StatusText = "Not watching project.";
        }
    }
}
