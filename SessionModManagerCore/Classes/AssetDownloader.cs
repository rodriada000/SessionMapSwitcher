using CG.Web.MegaApiClient;
using SessionModManagerCore.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SessionModManagerCore.Classes
{
    public class AssetDownloader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static AssetDownloader _instance;
        public static AssetDownloader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AssetDownloader();
                }

                return _instance;
            }
        }

        private CancellationTokenSource _megaDownloadCancelTokenSource;

        public void Download(DownloadItemViewModel newDownload)
        {
            if (!AssetCatalog.TryParseDownloadUrl(newDownload.DownloadUrl, out DownloadLocationType type, out string location))
            {
                return;
            }

            switch (type)
            {
                case DownloadLocationType.Url:
                    DownloadFileFromUrl(newDownload, location);
                    break;


                case DownloadLocationType.GDrive:
                    DownloadFileFromGDrive(newDownload, location);
                    break;


                case DownloadLocationType.MegaFile:
                    DownloadFileFromMega(newDownload, location);

                    break;

            }

            newDownload.IsStarted = true;
        }

        private void DownloadFileFromUrl(DownloadItemViewModel newDownload, string url)
        {
            using (var wc = new System.Net.WebClient())
            {
                newDownload.PerformCancel = () =>
                {
                    wc.CancelAsync();
                    newDownload.OnCancel?.Invoke();
                };
                wc.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(_wc_DownloadProgressChanged);
                wc.DownloadFileCompleted += new AsyncCompletedEventHandler(_wc_DownloadFileCompleted);
                wc.DownloadFileAsync(new Uri(url), newDownload.SaveFilePath, newDownload);
            }
        }

        private void DownloadFileFromGDrive(DownloadItemViewModel newDownload, string fileId)
        {
            GDrive gd = new GDrive();
            newDownload.PerformCancel = () =>
            {
                gd.CancelAsync();
                newDownload.OnCancel?.Invoke();
            };
            gd.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(_wc_DownloadProgressChanged);
            gd.DownloadFileCompleted += new AsyncCompletedEventHandler(_wc_DownloadFileCompleted);
            gd.Download(fileId, newDownload.SaveFilePath, newDownload);
        }

        private void DownloadFileFromMega(DownloadItemViewModel newDownload, string megaFileId)
        {
            bool wasCanceled = false;

            newDownload.PerformCancel = () =>
            {
                wasCanceled = true;

                try
                {
                    _megaDownloadCancelTokenSource?.Cancel();
                }
                catch (Exception dex)
                {
                    Logger.Error(dex);
                }
                finally
                {
                    _megaDownloadCancelTokenSource?.Dispose();
                    _megaDownloadCancelTokenSource = null;
                }

                newDownload.OnCancel?.Invoke();
            };

            var client = new MegaApiClient();
            client.LoginAnonymousAsync().ContinueWith((loginResult) =>
            {
                if (wasCanceled)
                {
                    return; // don't continue after async login since user already canceled download
                }

                if (loginResult.IsFaulted)
                {
                    newDownload.OnError?.Invoke(loginResult.Exception.GetBaseException());
                    return;
                }


                // get nodes from mega folder
                Uri fileLink = new Uri($"https://mega.nz/file/{megaFileId}");
                INode node = client.GetNodeFromLink(fileLink);

                if (wasCanceled)
                {
                    return; // don't continue after async login since user already canceled download
                }

                if (node == null)
                {
                    newDownload.OnError?.Invoke(new Exception($"could not find node from link {fileLink}"));
                    client.LogoutAsync();
                    return;
                }

                if (File.Exists(newDownload.SaveFilePath))
                {
                    File.Delete(newDownload.SaveFilePath); //delete old temp file if it exists (throws exception otherwise)
                }


                IProgress<double> progressHandler = new Progress<double>(x =>
                {
                    double estimatedBytesReceived = (double)node.Size * (x / 100);
                    UpdateDownloadProgress(newDownload, (int)x, (long)estimatedBytesReceived);
                });

                _megaDownloadCancelTokenSource = new CancellationTokenSource();
                Task downloadTask = client.DownloadFileAsync(fileLink, newDownload.SaveFilePath, progressHandler, _megaDownloadCancelTokenSource.Token);

                downloadTask.ContinueWith((downloadResult) =>
                {
                    _megaDownloadCancelTokenSource?.Dispose();
                    _megaDownloadCancelTokenSource = null;
                    client.LogoutAsync();

                    if (downloadResult.IsCanceled)
                    {
                        return;
                    }

                    if (downloadResult.IsFaulted)
                    {
                        newDownload.OnError?.Invoke(downloadResult.Exception.GetBaseException());
                        return;
                    }

                    _wc_DownloadFileCompleted(client, new AsyncCompletedEventArgs(null, false, newDownload));
                });
            });
        }

        void _wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DownloadItemViewModel item = (DownloadItemViewModel)e.UserState;
            if (e.Cancelled)
            {
                if (sender is System.Net.WebClient)
                {
                    (sender as System.Net.WebClient).Dispose();
                }
                item.OnCancel?.Invoke();
            }
            else if (e.Error != null)
            {
                item.OnError?.Invoke(e.Error);
            }
            else
            {
                item.OnComplete?.Invoke();
            }
        }

        void _wc_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            DownloadItemViewModel item = (DownloadItemViewModel)e.UserState;
            int prog = e.ProgressPercentage;
            if ((e.TotalBytesToReceive < 0) && (sender is GDrive))
            {
                prog = (int)(100 * e.BytesReceived / (sender as GDrive).GetContentLength());
            }
            UpdateDownloadProgress(item, prog, e.BytesReceived);
        }

        private void UpdateDownloadProgress(DownloadItemViewModel item, int percentDone, long bytesReceived)
        {
            if (item.PercentComplete != percentDone)
            {
                item.PercentComplete = percentDone;
            }

            TimeSpan interval = DateTime.Now - item.LastCalc;

            if ((interval.TotalSeconds >= 5))
            {
                if (bytesReceived > 0)
                {
                    double b = (bytesReceived - item.LastBytes) / 1024.0;
                    string uom = "KB/s";
                    if (b > 1024.0)
                    {
                        b /= 1024.0;
                        uom = "MB/s";
                    }
                    item.DownloadSpeed = (b / interval.TotalSeconds).ToString("0.0") + uom;
                    item.LastBytes = bytesReceived;
                }

                item.LastCalc = DateTime.Now;
            }
        }

    }
}
