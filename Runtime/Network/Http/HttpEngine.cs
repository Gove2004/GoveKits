using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace GoveKits.Runtime.Network
{
    internal static class HttpEngine
    {
        public static async UniTask<HttpResponse> ExecuteAsync(HttpRequestBuilder req, CancellationToken ct)
        {
            string finalUrl = BuildFinalUrl(req);

            // 管线 1：检查缓存
            if (req.UseCache && req.Method == HttpMethod.GET && HttpCore.Cache.TryGet(finalUrl, out string cachedText))
            {
                return HttpResponse.Cached(cachedText);
            }

            // 管线 2：并发控制 (使用该 Core 实例的锁)
            await HttpCore.Throttle.WaitAsync(ct);
            try
            {
                // 管线 3：执行请求
                return await ExecuteWithRetryAsync(finalUrl, req, ct);
            }
            finally
            {
                HttpCore.Throttle.Release();
            }
        }

        private static async UniTask<HttpResponse> ExecuteWithRetryAsync(string url, HttpRequestBuilder req, CancellationToken ct)
        {
            int attempts = 0;
            int maxAttempts = 1 + req.RetryCount;

            while (attempts < maxAttempts)
            {
                attempts++;
                using UnityWebRequest uwr = CreateRequest(url, req);
                
                try
                {
                    await uwr.SendWebRequest().WithCancellation(ct);

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        var response = HttpResponse.Success(uwr);
                        if (req.UseCache && req.Method == HttpMethod.GET && !string.IsNullOrEmpty(response.Text))
                            HttpCore.Cache.Set(url, response.Text);

                        return response;
                    }

                    bool isTransientError = uwr.result == UnityWebRequest.Result.ConnectionError || uwr.responseCode >= 500;
                    if (!isTransientError || attempts >= maxAttempts) return HttpResponse.Error(uwr);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    if (attempts >= maxAttempts) return HttpResponse.FailException(ex);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);
            }

            return HttpResponse.FailException(new Exception("Unknown error in retry loop."));
        }

        private static UnityWebRequest CreateRequest(string url, HttpRequestBuilder req)
        {
            var uwr = new UnityWebRequest(url, req.Method.ToString());
            if (req.BodyData != null)
            {
                string json = req.BodyData is string s ? s : JsonConvert.SerializeObject(req.BodyData);
                uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                uwr.SetRequestHeader("Content-Type", "application/json");
            }
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.timeout = (int)req.Timeout;

            if (req.Headers != null)
            {
                foreach (var kv in req.Headers) uwr.SetRequestHeader(kv.Key, kv.Value);
            }
            return uwr;
        }

        private static string BuildFinalUrl(HttpRequestBuilder req)
        {
            string url = req.Url;
            if (req.QueryParams == null || req.QueryParams.Count == 0) return url;

            var sb = new StringBuilder(url);
            sb.Append(url.Contains("?") ? "&" : "?");
            
            bool first = true;
            foreach (var kv in req.QueryParams)
            {
                if (!first) sb.Append("&");
                sb.Append($"{UnityWebRequest.EscapeURL(kv.Key)}={UnityWebRequest.EscapeURL(kv.Value)}");
                first = false;
            }
            return sb.ToString();
        }
    }
    
}