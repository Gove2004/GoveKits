using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace GoveKits.Network
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

        // --- 并发与缓存 ---
        private static readonly SemaphoreSlim _gate = new SemaphoreSlim(CONST_MAX_CONCURRENT);
        private static readonly ConcurrentDictionary<string, (string data, long expire)> _cache 
            = new ConcurrentDictionary<string, (string, long)>();

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
            // 1. URL 自动补全
            string finalUrl = req.Url.StartsWith("http") ? req.Url : $"{CONST_BASE_URL.TrimEnd('/')}/{req.Url.TrimStart('/')}";
            string cacheKey = null;

            // 2. 读缓存 (仅GET)
            if (req.UseCache && req.Method == HttpMethod.GET)
            {
                cacheKey = finalUrl;
                if (_cache.TryGetValue(cacheKey, out var item) && DateTime.UtcNow.Ticks < item.expire)
                {
                    return new ResponseData(true, 200, null, item.data, null);
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
                                long exp = DateTime.UtcNow.AddSeconds(300).Ticks;
                                _cache[cacheKey] = (text, exp);
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
    }
}