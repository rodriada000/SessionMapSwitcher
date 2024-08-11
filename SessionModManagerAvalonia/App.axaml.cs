using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Security.Principal;
using System;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;

namespace SessionModManagerAvalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static Version GetAppVersion()
        {
            return typeof(SessionModManagerAvalonia.App).Assembly.GetName().Version;
        }

        public static bool IsRunningAppAsAdministrator()
        {
            return false;
            // reference: https://stackoverflow.com/questions/11660184/c-sharp-check-if-run-as-administrator
            //return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
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

            return "Session Mod Manager"; // default if can't find for some reason
        }
    }
}