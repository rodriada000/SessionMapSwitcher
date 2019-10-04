
using SessionMapSwitcher.Utils;
using System;
using System.Diagnostics;
using System.Windows;

public class DownloadableMap : ViewModelBase
{
    private string _mapName;
    public string DownloadUrl;
    public string PreviewUrl;
    public string ImageUrl;

    public string MapName
    {
        get { return _mapName; }
        set
        {
            _mapName = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Name of zip file that will be downloaded for this map with the '.zip' extension
    /// (same as MapName with spaces replaced with '_')
    /// </summary>
    public string ZipFileName
    {
        get
        {
            return $"{MapName.Replace(" ", "_")}.zip";
        }
    }

    internal string DownloadUrlWithPrefixRemoved
    {
        get
        {
            if (IsDownloadUrlDirect())
            {
                return DownloadUrl.Substring("DIRECT:".Length);
            }
            return DownloadUrl;
        }
    }

    internal void OpenPreviewUrlInBrowser()
    {
        if (String.IsNullOrEmpty(PreviewUrl))
        {
            MessageBox.Show("No preview url for this map.", "Notice!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        ProcessStartInfo info = new ProcessStartInfo()
        {
            FileName = this.PreviewUrl
        };

        Process.Start(info);
    }

    internal void BeginDownloadInBrowser()
    {
        if (String.IsNullOrEmpty(DownloadUrl))
        {
            MessageBox.Show("No direct download url for this map.", "Notice!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        try
        {
            string directDownloadLink = DownloadUtils.GetDirectDownloadLinkFromAnonPage(this.DownloadUrl);
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = directDownloadLink
            };

            Process.Start(info);
        }
        catch (Exception e)
        {
            System.Windows.MessageBox.Show($"Could not initiate download from browser: {e.Message}", "Notice!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

    }

    internal bool IsDownloadUrlDirect()
    {
        return DownloadUrl.StartsWith("DIRECT:");
    }

}