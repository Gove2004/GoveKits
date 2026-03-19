using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace GoveKits.Runtime.Network.Protocol
{
    /// <summary>
    /// 轻量 HTTP 管理器：提供并发限制、可选 GET 缓存与重试策略。
    /// 使用 <see cref="RequestData"/> 构建请求并通过 <see cref="Send(RequestData, System.Threading.CancellationToken)"/> 发送。
    /// </summary>
    public static class WebAPI
    {
        // --- 静态常量配置 ---
        /// <summary>默认基础地址（相对 URL 将与其拼接）。</summary>
        public const string CONST_BASE_URL = "https://api.example.com";  // 默认基地址
        /// <summary>默认超时时间（秒）。</summary>
        public const float CONST_TIMEOUT = 15f;                          // 默认超时 (秒)
        /// <summary>默认最大重试次数（网络错误或 5xx）。</summary>
        public const int CONST_RETRY = 3;                                // 默认重试次数
        /// <summary>最大并发请求数。</summary>
        public const int CONST_MAX_CONCURRENT = 5;                       // 最大并发请求数
        /// <summary>默认缓存有效期（秒）。</summary>
        public const int CONST_CACHE_TTL_SECONDS = 300;
        /// <summary>缓存最大条目数，超过后触发轻量裁剪。</summary>
        public const int CONST_CACHE_MAX_COUNT = 512;
        /// <summary>每 N 次写缓存触发一次清理。</summary>
        public const int CONST_CACHE_SWEEP_INTERVAL = 32;

        // --- 并发与缓存 ---
        private static readonly SemaphoreSlim _gate = new SemaphoreSlim(CONST_MAX_CONCURRENT);
        private static readonly ConcurrentDictionary<string, (string data, long expire)> _cache 
            = new ConcurrentDictionary<string, (string, long)>();
        private static int _cacheWriteCounter;

        /// <summary>
        /// 发送请求（通用入口）。
        /// - 若 <see cref="RequestData.Url"/> 为相对路径，将自动与 <see cref="CONST_BASE_URL"/> 合并。
        /// - GET 且启用缓存时，优先命中缓存；命中缓存则直接返回。
        /// </summary>
        /// <param name="req">请求配置。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>响应数据。</returns>
        public static async UniTask<ResponseData> Send(RequestData req, CancellationToken ct = default)
        {
            if (req == null)
            {
                return ResponseData.Fail("RequestData is null.");
            }

            if (string.IsNullOrWhiteSpace(req.Url))
            {
                return ResponseData.Fail("RequestData.Url is null or empty.");
            }

            if (!TryResolveFinalUrl(req, out string finalUrl, out string urlError))
            {
                return ResponseData.Fail(urlError);
            }

            string cacheKey = null;

            // 2. 读缓存 (仅GET)
            if (req.UseCache && req.Method == HttpMethod.GET)
            {
                cacheKey = finalUrl;
                if (_cache.TryGetValue(cacheKey, out var item))
                {
                    if (DateTime.UtcNow.Ticks < item.expire)
                    {
                        return new ResponseData(true, 200, null, item.data, null);
                    }

                    _cache.TryRemove(cacheKey, out _);
                }
            }

            // 3. 并发控制
            await _gate.WaitAsync(ct);

            try
            {
                // 4. 执行请求
                return await ExecInternal(finalUrl, req, cacheKey, ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static async UniTask<ResponseData> ExecInternal(string url, RequestData req, string cacheKey, CancellationToken ct)
        {
            int retryLeft = req.Retry;

            while (true)
            {
                using (var uwr = CreateUWR(url, req))
                {
                    try
                    {
                        // 异步发送
                        await uwr.SendWebRequest().WithCancellation(ct);

                        // 成功
                        if (uwr.result == UnityWebRequest.Result.Success)
                        {
                            string text = uwr.downloadHandler?.text;
                            
                            // 写缓存 (5分钟)
                            if (cacheKey != null && !string.IsNullOrEmpty(text))
                            {
                                WriteCache(cacheKey, text);
                            }

                            return new ResponseData(true, uwr.responseCode, null, text, uwr.downloadHandler?.data);
                        }

                        // 失败检查
                        // 仅重试: 5xx(服务器错误) 或 ConnectionError(断网)
                        bool shouldRetry = uwr.responseCode >= 500 || uwr.result == UnityWebRequest.Result.ConnectionError;
                        
                        if (!shouldRetry || retryLeft <= 0)
                        {
                            return new ResponseData(false, uwr.responseCode, uwr.error, uwr.downloadHandler?.text, null);
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        if (retryLeft <= 0) return ResponseData.Fail(ex.Message);
                    }
                }

                retryLeft--;
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);
            }
        }

        private static UnityWebRequest CreateUWR(string url, RequestData req)
        {
            UnityWebRequest uwr = new UnityWebRequest(url, req.Method.ToString());
            
            // 设置 UploadHandler (如果有数据)
            if (req._bodyBytes != null && req._bodyBytes.Length > 0)
            {
                uwr.uploadHandler = new UploadHandlerRaw(req._bodyBytes);
                if (!string.IsNullOrEmpty(req._contentType))
                {
                    uwr.SetRequestHeader("Content-Type", req._contentType);
                }
            }

            // 设置 DownloadHandler
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.timeout = (int)req.Timeout;

            // 设置 Headers
            if (req.Headers != null)
            {
                foreach (var kv in req.Headers) uwr.SetRequestHeader(kv.Key, kv.Value);
            }

            return uwr;
        }
        
        /// <summary>
        /// 清空所有 GET 响应缓存。
        /// </summary>
        public static void ClearCache() => _cache.Clear();

        private static bool TryResolveFinalUrl(RequestData req, out string finalUrl, out string error)
        {
            finalUrl = null;
            error = null;

            string rawUrl = req.Url.Trim();
            if (Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri absoluteUri))
            {
                if (!IsHttpScheme(absoluteUri))
                {
                    error = $"Unsupported URL scheme: {absoluteUri.Scheme}";
                    return false;
                }

                finalUrl = absoluteUri.ToString();
                return true;
            }

            string baseUrl = string.IsNullOrWhiteSpace(req.BaseUrl) ? CONST_BASE_URL : req.BaseUrl.Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri baseUri) || !IsHttpScheme(baseUri))
            {
                error = $"Invalid BaseUrl: {baseUrl}";
                return false;
            }

            if (!Uri.TryCreate(baseUri, rawUrl, out Uri mergedUri) || !IsHttpScheme(mergedUri))
            {
                error = $"Invalid request URL: {rawUrl}";
                return false;
            }

            finalUrl = mergedUri.ToString();
            return true;
        }

        private static bool IsHttpScheme(Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private static void WriteCache(string key, string text)
        {
            long expire = DateTime.UtcNow.AddSeconds(CONST_CACHE_TTL_SECONDS).Ticks;
            _cache[key] = (text, expire);

            int writeCount = Interlocked.Increment(ref _cacheWriteCounter);
            if (writeCount % CONST_CACHE_SWEEP_INTERVAL == 0)
            {
                SweepCache();
            }
        }

        private static void SweepCache()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            foreach (var item in _cache)
            {
                if (item.Value.expire <= nowTicks)
                {
                    _cache.TryRemove(item.Key, out _);
                }
            }

            int count = _cache.Count;
            if (count <= CONST_CACHE_MAX_COUNT)
            {
                return;
            }

            var snapshot = new List<KeyValuePair<string, (string data, long expire)>>(_cache);
            snapshot.Sort((a, b) => a.Value.expire.CompareTo(b.Value.expire));

            int removeCount = snapshot.Count - CONST_CACHE_MAX_COUNT;
            for (int i = 0; i < removeCount; i++)
            {
                _cache.TryRemove(snapshot[i].Key, out _);
            }
        }
    }
}