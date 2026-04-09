using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    internal static class FtpEngine
    {
        public static async UniTask<FtpResponse> ExecuteAsync(
            FTPCore core,
            FtpRequestBuilder req, 
            string ftpMethod, 
            byte[] uploadData, 
            string savePath, 
            CancellationToken ct)
        {
            string finalUrl = req.Url;
            int attempts = 0;
            int maxAttempts = 1 + req.RetryCount;

            while (attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(finalUrl);
                    request.Method = ftpMethod;
                    request.Credentials = req.Credentials;
                    request.Timeout = (int)(req.Timeout * 1000);
                    request.ReadWriteTimeout = request.Timeout;
                    request.UsePassive = req.UsePassiveMode;
                    request.KeepAlive = false;

                    // 处理上传
                    if (uploadData != null && uploadData.Length > 0 && ftpMethod == WebRequestMethods.Ftp.UploadFile)
                    {
                        // 扔进线程池，防止 IO 阻塞主线程
                        using Stream requestStream = await UniTask.RunOnThreadPool(async () => await request.GetRequestStreamAsync());
                        await requestStream.WriteAsync(uploadData, 0, uploadData.Length, ct);
                    }

                    // 【核心优化】：将 FTP 请求的响应丢入线程池，防止 Unity 假死！
                    using FtpWebResponse response = (FtpWebResponse)await UniTask.RunOnThreadPool(
                        async () => await request.GetResponseAsync()
                    );
                    using Stream responseStream = response.GetResponseStream();

                    byte[] downloadedBytes = null;
                    string downloadedText = null;

                    if (responseStream != null && 
                        (ftpMethod == WebRequestMethods.Ftp.DownloadFile || 
                         ftpMethod == WebRequestMethods.Ftp.ListDirectory || 
                         ftpMethod == WebRequestMethods.Ftp.ListDirectoryDetails))
                    {
                        if (!string.IsNullOrEmpty(savePath))
                        {
                            using FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                            await responseStream.CopyToAsync(fs, 81920, ct); 
                        }
                        else
                        {
                            using MemoryStream ms = new MemoryStream();
                            await responseStream.CopyToAsync(ms, 81920, ct);
                            downloadedBytes = ms.ToArray();
                            downloadedText = Encoding.UTF8.GetString(downloadedBytes);
                        }
                    }

                    return FtpResponse.Success(response, downloadedBytes, downloadedText);
                }
                catch (OperationCanceledException) { throw; }
                catch (WebException webEx)
                {
                    if (attempts >= maxAttempts)
                    {
                        if (webEx.Response is FtpWebResponse ftpRes)
                            return FtpResponse.Error(ftpRes, webEx.Message);
                        else
                            return FtpResponse.FailException(webEx);
                    }
                }
                catch (Exception ex)
                {
                    if (attempts >= maxAttempts) return FtpResponse.FailException(ex);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);
            }

            return FtpResponse.FailException(new Exception("Unknown FTP retry loop error."));
        }
    }
}