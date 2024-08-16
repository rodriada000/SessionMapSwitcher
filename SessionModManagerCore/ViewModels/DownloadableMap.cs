using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.ViewModels;
using System;
using System.Diagnostics;

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

    public BoolWithMessage OpenPreviewUrlInBrowser()
    {
        if (String.IsNullOrEmpty(PreviewUrl))
        {
            return BoolWithMessage.False("No preview url for this map.");
        }

        ProcessStartInfo info = new ProcessStartInfo()
        {
            FileName = this.PreviewUrl
        };

        Process.Start(info);
        return BoolWithMessage.True();
    }

    public BoolWithMessage BeginDownloadInBrowser()
    {
        if (String.IsNullOrEmpty(DownloadUrl))
        {
            return BoolWithMessage.False("No direct download url for this map.");
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
            return BoolWithMessage.False($"Could not initiate download from browser: {e.Message}");
        }

        return BoolWithMessage.True();
    }

    public bool IsDownloadUrlDirect()
    {
        return DownloadUrl.StartsWith("DIRECT:");
    }

}