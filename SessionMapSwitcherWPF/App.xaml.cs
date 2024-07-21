using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherWPF.Classes;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows;

namespace SessionMapSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string sessionPath = AppSettingsUtil.GetAppSetting(SettingKey.PathToSession);

            if (string.IsNullOrEmpty(sessionPath))
            {
                sessionPath = RegistryHelper.GetPathFromRegistry();
            }

            SessionPath.ToSession = sessionPath;


#if DEBUG
            NLog.LogManager.ThrowExceptions = true;
#endif

            LogAppNameAndVersion();


        }

        private static void LogAppNameAndVersion()
        {
            string logLine = $"{GetAppName()} - {GetAppVersion()}";

            logLine += IsRunningAppAsAdministrator() ? " (Running As Admin)" : " (Running As Normal User)";

            Logger.Info("----------------------------------------------------------------------------------------------------");
            Logger.Info(logLine);
        }
    }
}
