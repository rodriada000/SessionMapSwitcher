using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SessionMapSwitcher.Utils
{
    class DownloadUtils
    {
        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public static event ProgressChangedHandler ProgressChanged;

        /// <summary>
        /// Make a request to the given url and return the response as a string.
        /// Used for getting .txt documents from github.
        /// </summary>
        public static string GetTxtDocumentFromGitHubRepo(string githubUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> task = client.GetAsync(githubUrl);
                task.Wait();

                HttpResponseMessage response = task.Result;
                // Check that response was successful or throw exception
                response.EnsureSuccessStatusCode();

                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait();

                return readTask.Result;
            }
        }

        /// <summary>
        /// Make request to anonfile download page and scrape direct download url from it.
        /// Will throw an exception if http request fails
        /// </summary>
        public static string GetDirectDownloadLinkFromAnonPage(string anonFileUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> requestTask = client.GetAsync(anonFileUrl);
                requestTask.Wait();

                HttpResponseMessage response = requestTask.Result;
                // Check that response was successful or throw exception
                response.EnsureSuccessStatusCode();

                var readTask = response.Content.ReadAsStringAsync();
                readTask.Wait();

                string html = readTask.Result;
                string downloadUrl = FindDirectDownloadUrlInHtml(html);

                return downloadUrl;
            }
        }

        /// <summary>
        /// Scrapes the anonfile html for the direct download link of a file
        /// </summary>
        private static string FindDirectDownloadUrlInHtml(string html)
        {
            Regex regex = new Regex("<a type=\"button\" id=\"download-url\"[.\\s]*.*\\s*href.*\">");
            Match match = regex.Match(html);

            if (match.Success)
            {
                string downloadUrl = match.Value.TrimEnd(new char[] { '\"', '>', '<' });

                string hrefStr = "href=\"";
                int index = downloadUrl.IndexOf(hrefStr);

                downloadUrl = downloadUrl.Substring(index + hrefStr.Length);

                return downloadUrl;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Downloads a file from a given URL and saves it to the given path.
        /// </summary>
        public static async Task DownloadFileToFolderAsync(string downloadUrl, string savePath, CancellationToken cancelToken, bool noTimeout = false)
        {
            using (HttpClient client = new HttpClient())
            {
                if (noTimeout)
                {
                    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                }

                if (cancelToken != CancellationToken.None)
                {
                    cancelToken.Register(() =>
                    {
                        client.CancelPendingRequests();
                        cancelToken.ThrowIfCancellationRequested();
                    });
                }


                using (HttpResponseMessage response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancelToken))
                {
                    long? totalBytes = response.Content.Headers.ContentLength;

                    // You must use as stream to have control over buffering and number of bytes read/received
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        await ProcessContentStreamAsync(totalBytes, stream, savePath, cancelToken);
                    }
                }
            }
        }

        private static async Task ProcessContentStreamAsync(long? totalDownloadSize, Stream contentStream, string destinationFilePath, CancellationToken token)
        {
            // reference: https://stackoverflow.com/questions/20661652/progress-bar-with-httpclient
            int bufferSize = 8192 * 2;
            long totalBytesRead = 0L;
            long readCount = 0L;
            byte[] buffer = new byte[bufferSize];
            bool isMoreToRead = true;

            using (FileStream fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                do
                {
                    int bytesRead;
                    bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        continue;
                    }

                    await fileStream.WriteAsync(buffer, 0, bytesRead, token);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (readCount % 5 == 0)
                        TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                }
                while (isMoreToRead);

            }

            //the last progress trigger should occur after the file handle has been released or you may get file locked error
            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
        }

        private static void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
            {
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
            }
            else
            {
                totalDownloadSize = 0;
                progressPercentage = 0;
            }

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }


    }
}
