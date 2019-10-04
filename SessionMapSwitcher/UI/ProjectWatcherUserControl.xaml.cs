using SessionMapSwitcher.ViewModels;
using System.Windows.Controls;

namespace SessionMapSwitcher.UI
{
    /// <summary>
    /// Interaction logic for ProjectWatcherUserControl.xaml
    /// </summary>
    public partial class ProjectWatcherUserControl : UserControl
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
            ViewModel.BrowseForProject();
        }

        private void BtnWatch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.WatchProject();   
        }

        private void BtnUnwatch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.UnwatchProject();
        }
    }
}
