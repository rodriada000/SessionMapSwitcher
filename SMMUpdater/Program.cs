using System.Diagnostics;
using System.Reflection;

string GetApplicationRoot()
{
    if (OperatingSystem.IsWindows())
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    return Path.GetDirectoryName(Environment.GetCommandLineArgs().FirstOrDefault());
}

bool GetNoLaunchArg()
{
    var args = Environment.GetCommandLineArgs();
    return (args.Length > 1 && args[1] == "-nolaunch");
}



/// KEEP THIS REAL SIMPLE
/// assuming zip file is already downloaded named "latest_smm.zip" and extracted 

List<string> excludedFiles = new List<string>() { "appsettings.json", "SMMUpdater.exe", "SMMUpdater.dll", "SMMUpdater" };
string pathToReleaseZip = Path.Combine(GetApplicationRoot(), "latest_smm.zip");

DirectoryInfo? appDir = new DirectoryInfo(GetApplicationRoot());
DirectoryInfo? parentDir = appDir.Parent;

IProgress<double> progress = new Progress<double>(percent => Console.Write($"."));

Console.WriteLine($"------------- SMM Updater v{Assembly.GetEntryAssembly().GetName().Version} -------------");
Console.WriteLine($"Updater Folder: {GetApplicationRoot()}");
Console.WriteLine($"SMM Folder: {parentDir?.FullName}");
Console.WriteLine("---------------------------------------");

try
{
    if (File.Exists(pathToReleaseZip) && Directory.Exists(Path.Combine(GetApplicationRoot(), "Session Mod Manager")))
    {

        // close SMM if running
        Process? process = Process.GetProcessesByName("SessionModManager").FirstOrDefault();
        if (process != null && process.Responding)
        {
            Console.WriteLine($"Closing application before continuing ...");
            process.Kill();
            await process.WaitForExitAsync();
            await Task.Delay(1000);
        }

        Console.WriteLine($"latest_smm.zip found ...");
        await Task.Delay(1000);
        Console.Write($"copying new version files ...");
        await Task.Delay(1000);

        // copy new files to mod manager directory
        bool didCopy = FileUtils.CopyDirectoryRecursively(Path.Combine(GetApplicationRoot(), "Session Mod Manager"), parentDir.FullName, excludedFiles, null, true, progress);

        if (!didCopy)
        {
            Console.WriteLine("Can not update app. Failed to copy files.");
            Console.WriteLine("Press Enter key to close ...");
            Console.ReadLine();
        }

        // relaunch app
        if (!GetNoLaunchArg())
        {
            Console.WriteLine("\nLaunching application ...");

            string exeName = Path.Combine(parentDir.FullName, OperatingSystem.IsWindows() ? "SessionModManager.exe" : "SessionModManager");
            Process.Start(new ProcessStartInfo() { WorkingDirectory = parentDir.FullName, FileName = exeName, UseShellExecute = false });
        }

        Console.WriteLine("\nSession Mod Manager successfully updated to the latest version ...");


        // cleanup downloaded files
        Directory.Delete(Path.Combine(GetApplicationRoot(), "Session Mod Manager"), true);
        File.Delete(pathToReleaseZip);

        await Task.Delay(2000);
    }
    else
    {
        Console.WriteLine("Can not update app. File or folder may be missing.");
        Console.WriteLine($"expected latest_smm.zip at {pathToReleaseZip}");
        Console.WriteLine($"expected Session Mod Manager folder at {Path.Combine(GetApplicationRoot(), "Session Mod Manager")}");
        Console.WriteLine("Press Enter key to close ...");
        Console.ReadLine();
    }
}
catch (Exception e)
{
    Console.WriteLine($"An error occurred trying to update the app - {e.Message}");
    Console.WriteLine("Press Enter key to close ...");
    Console.ReadLine();
}