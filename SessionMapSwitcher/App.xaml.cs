using SessionMapSwitcher.Utils;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Version GetAppVersion()
        {
            return typeof(SessionMapSwitcher.App).Assembly.GetName().Version;
        }

        public static bool IsRunningAppAsAdministrator()
        {
            // reference: https://stackoverflow.com/questions/11660184/c-sharp-check-if-run-as-administrator
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static string GetAppName()
        {
            foreach (object item in System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(false))
            {
                if (item is System.Reflection.AssemblyTitleAttribute)
                {
                    return (item as System.Reflection.AssemblyTitleAttribute).Title;
                }
            }

            return "Session Map Switcher"; // default if can't find for some reason
        }

        internal static void RestartAsAdminstrator()
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo()
                {
                    FileName = typeof(SessionMapSwitcher.App).Assembly.Location,
                    Verb = "runas"
                };

                Process adminProc = Process.Start(info);

                if (adminProc != null)
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to restart as administrator: {e.Message}.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string PathToSession { get; set; }

        public static string PathToSessionContent
        {
            get
            {
                return $"{PathToSession}\\SessionGame\\Content";
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            PathToSession = AppSettingsUtil.GetAppSetting("PathToSession");
        }
    }
}
