using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HTTP 请求执行引擎，采用三阶段管线模型。
    /// 阶段一：缓存命中检查（GET 且启用缓存时跳过网络请求）。
    /// 阶段二：并发请求节流（通过信号量限制同时进行的请求数）。
    /// 阶段三：实际网络请求执行（含自动重试与超时控制）。
    /// </summary>
    internal class HttpEngine
    {
        private readonly HttpCache _cache;
        private readonly SemaphoreSlim _throttle;
        private readonly float _defaultTimeout;
        private readonly int _defaultRetryCount;

        /// <summary>
        /// 创建 HTTP 请求执行引擎。
        /// </summary>
        /// <param name="cache">响应缓存实例。</param>
        /// <param name="throttle">并发请求信号量，用于限制同时进行的请求数。</param>
        /// <param name="defaultTimeout">默认超时时间（秒）。</param>
        /// <param name="defaultRetryCount">默认重试次数。</param>
        public HttpEngine(
            HttpCache cache,
            SemaphoreSlim throttle,
            float defaultTimeout = 15f,
            int defaultRetryCount = 3)
        {
            _cache = cache;
            _throttle = throttle;
            _defaultTimeout = defaultTimeout;
            _defaultRetryCount = defaultRetryCount;
        }

        /// <summary>
        /// 执行完整的 HTTP 请求管线：缓存检查、并发节流、网络请求与重试。
        /// </summary>
        /// <param name="req">已配置的 HTTP 请求构建器。</param>
        /// <param name="ct">用于取消请求的 CancellationToken。</param>
        /// <returns>包含响应状态码、数据和错误信息的 HttpResponse。</returns>
        public async UniTask<HttpResponse> ExecuteAsync(HttpRequestBuilder req, CancellationToken ct)
        {
            string finalUrl = BuildFinalUrl(req);

            // 阶段 1：缓存检查（仅 GET + 启用缓存）
            if (req.UseCache && req.Method == HttpMethod.GET && _cache.TryGet(finalUrl, out string cachedText))
            {
                return HttpResponse.Cached(cachedText);
            }

            // 阶段 2：并发节流
            await _throttle.WaitAsync(ct);
            try
            {
                // 阶段 3：执行请求（带重试）
                return await ExecuteWithRetryAsync(finalUrl, req, ct);
            }
            finally
            {
                _throttle.Release();
            }
        }

        private async UniTask<HttpResponse> ExecuteWithRetryAsync(string url, HttpRequestBuilder req, CancellationToken ct)
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
                        var response = HttpResponse.Success(uwr.responseCode, uwr.downloadHandler?.text);
                        // 成功且启用缓存时写入缓存
                        if (req.UseCache && req.Method == HttpMethod.GET && !string.IsNullOrEmpty(response.Text))
                        {
                            _cache.Set(url, response.Text);
                        }
                        return response;
                    }

                    // 连接错误或 5xx 视为瞬态错误，可重试
                    bool isTransientError = uwr.result == UnityWebRequest.Result.ConnectionError || uwr.responseCode >= 500;
                    if (!isTransientError || attempts >= maxAttempts)
                    {
                        return HttpResponse.Error(uwr.responseCode, uwr.error, uwr.downloadHandler?.text);
                    }
                }
                catch (OperationCanceledException)
                {
                    return HttpResponse.Error(0, "请求已取消", null);
                }
                catch (Exception ex)
                {
                    if (attempts >= maxAttempts)
                    {
                        return HttpResponse.FailException(ex);
                    }
                }

                // 重试前等待 1 秒
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);
            }

            return HttpResponse.FailException(new Exception("重试循环中出现未知错误。"));
        }

        private static UnityWebRequest CreateRequest(string url, HttpRequestBuilder req)
        {
            var uwr = new UnityWebRequest(url, req.Method.ToString());
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.timeout = Mathf.Max(1, Mathf.CeilToInt(req.Timeout));

            if (req.BodyData != null)
            {
                string json = req.BodyData is string s ? s : JsonConvert.SerializeObject(req.BodyData);
                uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                uwr.SetRequestHeader("Content-Type", "application/json");
            }

            if (req.Headers != null)
            {
                foreach (var kv in req.Headers)
                {
                    uwr.SetRequestHeader(kv.Key, kv.Value);
                }
            }

            return uwr;
        }

        private static string BuildFinalUrl(HttpRequestBuilder req)
        {
            string url = req.Url;
            if (req.QueryParams == null || req.QueryParams.Count == 0)
                return url;

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
