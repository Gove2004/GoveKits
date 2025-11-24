using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GoveKits.Network
{
    /// <summary>
    /// Static HTTP manager with queueing, caching and retry logic.
    /// Single exposed API: Request(RequestData) -> UniTask<ResponseData>
    /// </summary>
    public static class WebAPI
    {
        // configurable base URL and default timeout
        public static string BaseApiUrl = "https://api.example.com/";
        public static float DefaultTimeout = 10f;

        // concurrency / retry / cache settings (changeable by caller)
        public static int MaxConcurrentRequests { get; set; } = 5;
        public static int MaxRetryCount { get; set; } = 3;
        public static float RetryInterval { get; set; } = 1f;
        public static bool EnableCaching { get; set; } = true;
        public static float CacheDuration { get; set; } = 300f;

        // internal queue & cache
        private class QueueEntry
        {
            public RequestData Request;
            public UniTaskCompletionSource<ResponseData> Tcs;
            public CancellationToken Cancellation;
        }

        private static readonly Queue<QueueEntry> requestQueue = new Queue<QueueEntry>();
        private static int activeRequests = 0;
        private static readonly Dictionary<string, (string response, DateTime expiration)> responseCache = new Dictionary<string, (string, DateTime)>();
        private static readonly System.Diagnostics.Stopwatch cacheTimer = System.Diagnostics.Stopwatch.StartNew();

        // lock for queue operations
        private static readonly object queueLock = new object();
        private static readonly object cacheLock = new object();

        /// <summary>
        /// 发起HTTP请求，返回响应数据的异步任务。
        /// 支持请求排队、重试和响应缓存。
        /// </summary>
        /// <param name="config">请求配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>响应数据的异步任务</returns>
        public static UniTask<ResponseData> Request(RequestData config, CancellationToken cancellationToken = default)
        {
            var tcs = new UniTaskCompletionSource<ResponseData>();

            // 检查取消令牌
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return tcs.Task;
            }

            // 检查缓存
            if (config.useCache && EnableCaching && config.method == HttpMethod.GET)
            {
                var cacheKey = GenerateCacheKey(config);
                lock (cacheLock)
                {
                    if (responseCache.TryGetValue(cacheKey, out var entry))
                    {
                        if (DateTime.Now < entry.expiration)
                        {
                            tcs.TrySetResult(new ResponseData { success = true, statusCode = 200, text = entry.response });
                            return tcs.Task;
                        }
                        else
                        {
                            responseCache.Remove(cacheKey);
                        }
                    }
                }
            }

            var qe = new QueueEntry { Request = config, Tcs = tcs, Cancellation = cancellationToken };
            lock (queueLock)
            {
                requestQueue.Enqueue(qe);
            }

            // 不等待，直接触发队列处理
            ProcessQueueAsync();

            return tcs.Task;
        }

        private static void ProcessQueueAsync()
        {
            // 定期清理过期缓存（每60秒一次）
            if (cacheTimer.Elapsed.TotalSeconds > 60)
            {
                ClearExpiredCache();
                cacheTimer.Restart();
            }

            while (true)
            {
                QueueEntry entry = null;
                lock (queueLock)
                {
                    if (activeRequests >= MaxConcurrentRequests || requestQueue.Count == 0)
                        break;

                    entry = requestQueue.Dequeue();
                    activeRequests++;
                }

                if (entry == null) break;

                // 处理请求而不阻塞循环
                _ = ExecuteRequestAsync(entry); // 使用丢弃操作符，不等待
            }
        }

        private static async UniTask ExecuteRequestAsync(QueueEntry entry)
        {
            UnityWebRequest webRequest = null;
            try
            {
                var config = entry.Request;
                var cancellation = entry.Cancellation;

                // 再次检查取消
                if (cancellation.IsCancellationRequested)
                {
                    entry.Tcs.TrySetCanceled(cancellation);
                    return;
                }

                int timeoutMs = config.timeout > 0 ? (int)(config.timeout * 1000) : (int)(DefaultTimeout * 1000);
                int retryCount = config.retryCount >= 0 ? config.retryCount : MaxRetryCount;

                string url = CombineUrl(BaseApiUrl, config.endpoint);

                int attempts = 0;
                Exception lastException = null;
                bool success = false;
                ResponseData response = null;

                while (attempts <= retryCount && !success)
                {
                    if (attempts > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(RetryInterval), cancellationToken: cancellation);
                    }

                    try
                    {
                        webRequest = CreateWebRequest(url, config.method, config.body, config.headers);
                        webRequest.timeout = timeoutMs / 1000; // Unity使用秒为单位

                        using (var timeoutCts = new CancellationTokenSource(timeoutMs))
                        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, timeoutCts.Token))
                        {
                            var op = webRequest.SendWebRequest();
                            await op.WithCancellation(linkedCts.Token);
                        }

                        response = CreateResponse(webRequest);
                        
                        // 只对服务器错误(5xx)和网络错误重试，不对客户端错误(4xx)重试
                        if (response.success || (webRequest.responseCode >= 400 && webRequest.responseCode < 500))
                        {
                            success = true;
                        }
                        else
                        {
                            attempts++;
                            lastException = new Exception($"HTTP错误: {webRequest.responseCode}");
                            if (attempts <= retryCount)
                            {
                                webRequest.Dispose();
                                webRequest = null;
                                continue;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                    {
                        entry.Tcs.TrySetCanceled(cancellation);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        lastException = new TimeoutException("请求超时");
                        attempts++;
                        webRequest?.Dispose();
                        webRequest = null;
                        if (attempts > retryCount) break;
                        continue;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        attempts++;
                        webRequest?.Dispose();
                        webRequest = null;
                        if (attempts > retryCount) break;
                        continue;
                    }
                }

                if (success && response != null)
                {
                    // 缓存成功的GET请求
                    if (config.useCache && EnableCaching && config.method == HttpMethod.GET)
                    {
                        var cacheKey = GenerateCacheKey(config);
                        lock (cacheLock)
                        {
                            responseCache[cacheKey] = (response.text ?? "", DateTime.Now.AddSeconds(CacheDuration));
                        }
                    }
                    entry.Tcs.TrySetResult(response);
                }
                else
                {
                    entry.Tcs.TrySetResult(new ResponseData 
                    { 
                        success = false, 
                        error = lastException?.Message ?? "请求失败",
                        statusCode = webRequest?.responseCode ?? 0
                    });
                }
            }
            catch (Exception ex)
            {
                entry.Tcs.TrySetResult(new ResponseData 
                { 
                    success = false, 
                    error = $"未处理的异常: {ex.Message}" 
                });
            }
            finally
            {
                webRequest?.Dispose();
                
                lock (queueLock)
                {
                    activeRequests = Math.Max(0, activeRequests - 1);
                }
                
                // 继续处理队列中的项目
                ProcessQueueAsync();
            }
        }

        #region helpers
        private static UnityWebRequest CreateWebRequest(string url, HttpMethod method, object body, Dictionary<string, string> headers)
        {
            UnityWebRequest webRequest;
            string contentType = "application/json";
            bool hasCustomContentType = headers?.ContainsKey("Content-Type") == true;

            switch (method)
            {
                case HttpMethod.POST:
                    if (body is WWWForm form)
                    {
                        webRequest = UnityWebRequest.Post(url, form);
                    }
                    else
                    {
                        webRequest = new UnityWebRequest(url, "POST");
                        if (body != null)
                        {
                            if (body is string s)
                            {
                                var bytes = Encoding.UTF8.GetBytes(s);
                                webRequest.uploadHandler = new UploadHandlerRaw(bytes);
                                if (!hasCustomContentType)
                                    webRequest.SetRequestHeader("Content-Type", contentType);
                            }
                            else if (body is byte[] b)
                            {
                                webRequest.uploadHandler = new UploadHandlerRaw(b);
                            }
                        }
                        webRequest.downloadHandler = new DownloadHandlerBuffer();
                    }
                    break;

                case HttpMethod.PUT:
                    if (body is string putStr)
                    {
                        webRequest = UnityWebRequest.Put(url, putStr);
                        if (!hasCustomContentType)
                            webRequest.SetRequestHeader("Content-Type", contentType);
                    }
                    else if (body is byte[] putBytes)
                    {
                        webRequest = UnityWebRequest.Put(url, putBytes);
                    }
                    else
                    {
                        webRequest = UnityWebRequest.Put(url, "");
                    }
                    break;

                case HttpMethod.DELETE:
                    webRequest = UnityWebRequest.Delete(url);
                    break;

                case HttpMethod.PATCH:
                    webRequest = new UnityWebRequest(url, "PATCH");
                    if (body != null)
                    {
                        if (body is string pstr)
                        {
                            var bytes = Encoding.UTF8.GetBytes(pstr);
                            webRequest.uploadHandler = new UploadHandlerRaw(bytes);
                            if (!hasCustomContentType)
                                webRequest.SetRequestHeader("Content-Type", contentType);
                        }
                        else if (body is byte[] pb)
                        {
                            webRequest.uploadHandler = new UploadHandlerRaw(pb);
                        }
                    }
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    break;

                default:
                    // GET
                    if (body is Dictionary<string, string> query)
                    {
                        url = BuildUrlWithParams(url, query);
                    }
                    webRequest = UnityWebRequest.Get(url);
                    break;
            }

            // 确保有下载处理器
            if (webRequest.downloadHandler == null)
                webRequest.downloadHandler = new DownloadHandlerBuffer();

            // 设置请求头（尊重用户自定义的Content-Type）
            if (headers != null)
            {
                foreach (var h in headers)
                {
                    webRequest.SetRequestHeader(h.Key, h.Value);
                }
            }

            return webRequest;
        }

        private static string BuildUrlWithParams(string url, Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return url;
            var list = new List<string>();
            foreach (var p in parameters)
            {
                list.Add($"{UnityWebRequest.EscapeURL(p.Key)}={UnityWebRequest.EscapeURL(p.Value)}");
            }
            return url + (url.Contains("?") ? "&" : "?") + string.Join("&", list);
        }

        private static ResponseData CreateResponse(UnityWebRequest webRequest)
        {
            var response = new ResponseData
            {
                statusCode = (long)webRequest.responseCode,
                headers = ParseResponseHeaders(webRequest),
                bytes = webRequest.downloadHandler?.data,
                text = webRequest.downloadHandler?.text
            };

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                response.success = true;
            }
            else
            {
                response.success = false;
                response.error = webRequest.error;
                if (string.IsNullOrEmpty(response.error))
                {
                    response.error = webRequest.responseCode >= 400 ? 
                        $"HTTP错误: {webRequest.responseCode}" : "网络错误";
                }
            }

            return response;
        }

        private static Dictionary<string, string> ParseResponseHeaders(UnityWebRequest webRequest)
        {
            var headers = new Dictionary<string, string>();
            var rh = webRequest.GetResponseHeaders();
            if (rh == null) return headers;
            foreach (var kv in rh) headers[kv.Key] = kv.Value;
            return headers;
        }

        private static string CombineUrl(string baseUrl, string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return baseUrl.TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl)) return endpoint;
            
            baseUrl = baseUrl.TrimEnd('/');
            endpoint = endpoint.TrimStart('/');
            return $"{baseUrl}/{endpoint}";
        }

        private static string GenerateCacheKey(RequestData config)
        {
            var key = $"{config.method}:{CombineUrl(BaseApiUrl, config.endpoint)}";
            
            // 包含查询参数
            if (config.body is Dictionary<string, string> queryParams && queryParams.Count > 0)
            {
                var sortedParams = string.Join("&", queryParams.OrderBy(x => x.Key)
                    .Select(x => $"{x.Key}={x.Value}"));
                key += $"?{sortedParams}";
            }
            
            return key;
        }

        private static void ClearExpiredCache()
        {
            lock (cacheLock)
            {
                var expiredKeys = responseCache
                    .Where(x => DateTime.Now >= x.Value.expiration)
                    .Select(x => x.Key)
                    .ToList();
                    
                foreach (var key in expiredKeys)
                {
                    responseCache.Remove(key);
                }
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public static void ClearAllCache()
        {
            lock (cacheLock)
            {
                responseCache.Clear();
            }
        }

        /// <summary>
        /// 获取当前队列状态
        /// </summary>
        public static (int queued, int active) GetQueueStatus()
        {
            lock (queueLock)
            {
                return (requestQueue.Count, activeRequests);
            }
        }
        #endregion
    }
}