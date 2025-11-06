using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GoveKits.Web
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

        // lock for queue operations
        private static readonly object queueLock = new object();

        /// <summary>
        /// Public single entry point. Enqueues the request and returns a UniTask that completes when the request is processed.
        /// </summary>
        public static UniTask<ResponseData> Request(RequestData config, CancellationToken cancellationToken = default)
        {
            var tcs = new UniTaskCompletionSource<ResponseData>();

            // check cache immediately
            if (config.useCache && EnableCaching)
            {
                var cacheKey = GenerateCacheKey(config);
                lock (responseCache)
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

            // try to process queue (fire-and-forget)
            ProcessQueueAsync().Forget();

            return tcs.Task;
        }

        private static async UniTask ProcessQueueAsync()
        {
            while (true)
            {
                QueueEntry entry = null;
                lock (queueLock)
                {
                    if (activeRequests < MaxConcurrentRequests && requestQueue.Count > 0)
                    {
                        entry = requestQueue.Dequeue();
                        activeRequests++;
                    }
                }

                if (entry == null) break;

                // process entry without blocking loop
                ExecuteRequestAsync(entry).Forget();
            }
        }

        private static async UniTask ExecuteRequestAsync(QueueEntry entry)
        {
            try
            {
                var config = entry.Request;
                var cancellation = entry.Cancellation;

                int timeout = config.timeout > 0 ? (int)config.timeout : (int)DefaultTimeout;
                int retryCount = config.retryCount > 0 ? config.retryCount : MaxRetryCount;

                string url = CombineUrl(BaseApiUrl, config.endpoint);

                int attempts = 0;
                Exception lastException = null;
                UnityWebRequest webRequest = null;
                bool success = false;

                while (attempts <= retryCount && !success)
                {
                    if (attempts > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(RetryInterval), cancellationToken: cancellation);
                    }

                    try
                    {
                        webRequest = CreateWebRequest(url, config.method, config.body, config.headers);
                        webRequest.timeout = timeout;

                        var op = webRequest.SendWebRequest();
                        await op.WithCancellation(cancellation).Timeout(TimeSpan.FromSeconds(timeout));

                        if (IsRequestSuccess(webRequest))
                        {
                            success = true;
                        }
                        else
                        {
                            attempts++;
                            if (attempts > retryCount) break;
                            webRequest.Dispose();
                            webRequest = null;
                            continue;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        entry.Tcs.TrySetResult(new ResponseData { success = false, error = "请求被取消" });
                        return;
                    }
                    catch (TimeoutException)
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

                if (webRequest == null)
                {
                    entry.Tcs.TrySetResult(new ResponseData { success = false, error = lastException?.Message ?? "创建请求失败" });
                    return;
                }

                var response = CreateResponse(webRequest);

                if (success && config.useCache && EnableCaching)
                {
                    var cacheKey = GenerateCacheKey(config);
                    lock (responseCache)
                    {
                        responseCache[cacheKey] = (response.text ?? "", DateTime.Now.AddSeconds(CacheDuration));
                    }
                }

                entry.Tcs.TrySetResult(response);
            }
            finally
            {
                lock (queueLock)
                {
                    activeRequests = Math.Max(0, activeRequests - 1);
                }
                // try to continue processing queued items
                ProcessQueueAsync().Forget();
            }
        }

        #region helpers
        private static UnityWebRequest CreateWebRequest(string url, HttpMethod method, object body, Dictionary<string, string> headers)
        {
            UnityWebRequest webRequest;
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
                                webRequest.SetRequestHeader("Content-Type", "application/json");
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
                            webRequest.SetRequestHeader("Content-Type", "application/json");
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

            // ensure download handler
            if (webRequest.downloadHandler == null)
                webRequest.downloadHandler = new DownloadHandlerBuffer();

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
            return url + "?" + string.Join("&", list);
        }

        private static ResponseData CreateResponse(UnityWebRequest webRequest)
        {
            var response = new ResponseData
            {
                statusCode = webRequest.responseCode,
                headers = ParseResponseHeaders(webRequest),
                bytes = webRequest.downloadHandler?.data,
                text = webRequest.downloadHandler?.text
            };

            if (IsRequestSuccess(webRequest)) response.success = true;
            else
            {
                response.success = false;
                response.error = webRequest.error;
                if (string.IsNullOrEmpty(response.error)) response.error = $"HTTP错误: {webRequest.responseCode}";
            }

            return response;
        }

        private static bool IsRequestSuccess(UnityWebRequest webRequest)
        {
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                return false;
            return webRequest.responseCode >= 200 && webRequest.responseCode < 300;
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
            if (string.IsNullOrEmpty(endpoint)) return baseUrl;
            if (baseUrl.EndsWith("/") && endpoint.StartsWith("/")) return baseUrl + endpoint.Substring(1);
            if (!baseUrl.EndsWith("/") && !endpoint.StartsWith("/")) return baseUrl + "/" + endpoint;
            return baseUrl + endpoint;
        }

        private static string GenerateCacheKey(RequestData config)
        {
            return $"{config.method}:{CombineUrl(BaseApiUrl, config.endpoint)}";
        }
        #endregion
    }
}