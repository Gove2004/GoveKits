using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HTTP 门面（v3.0.0 起基于 UnityWebRequest + UniTask，不再自研引擎/缓存/构建器）。
    /// 所有方法均为异步，统一返回 <see cref="HttpResponse"/>，支持超时与取消（CancellationToken）。
    /// </summary>
    public static class HttpCore
    {
        /// <summary>默认请求超时（秒）。</summary>
        public const float DefaultTimeout = 30f;

        /// <summary>最大并发请求数（超出后排队等待）。</summary>
        public const int MaxConcurrent = 8;

        /// <summary>并发信号量，避免 UWR 请求数过多。</summary>
        private static readonly System.Threading.SemaphoreSlim _throttle = new(MaxConcurrent);

        /// <summary>发起 GET 请求。</summary>
        public static UniTask<HttpResponse> GetAsync(string url, Dictionary<string, string> headers = null, float timeout = DefaultTimeout, CancellationToken cancellationToken = default)
            => SendAsync(UnityWebRequest.Get(url), headers, timeout, cancellationToken);

        /// <summary>发起 POST 请求（原始字节体）。</summary>
        public static UniTask<HttpResponse> PostAsync(string url, byte[] body, string contentType = "application/octet-stream",
            Dictionary<string, string> headers = null, float timeout = DefaultTimeout, CancellationToken cancellationToken = default)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body ?? Array.Empty<byte>()),
                downloadHandler = new DownloadHandlerBuffer()
            };
            if (!string.IsNullOrEmpty(contentType))
                request.SetRequestHeader("Content-Type", contentType);
            return SendAsync(request, headers, timeout, cancellationToken);
        }

        /// <summary>发起 POST 请求（JSON 字符串体）。</summary>
        public static UniTask<HttpResponse> PostJsonAsync(string url, string json,
            Dictionary<string, string> headers = null, float timeout = DefaultTimeout, CancellationToken cancellationToken = default)
            => PostAsync(url, Encoding.UTF8.GetBytes(json ?? "{}"), "application/json", headers, timeout, cancellationToken);

        /// <summary>发起 POST 请求（对象自动序列化为 JSON）。</summary>
        public static UniTask<HttpResponse> PostJsonAsync(string url, object payload,
            Dictionary<string, string> headers = null, float timeout = DefaultTimeout, CancellationToken cancellationToken = default)
            => PostJsonAsync(url, JsonConvert.SerializeObject(payload), headers, timeout, cancellationToken);

        /// <summary>发起 PUT 请求（JSON 字符串体）。</summary>
        public static UniTask<HttpResponse> PutAsync(string url, byte[] body, string contentType = "application/octet-stream",
            Dictionary<string, string> headers = null, float timeout = DefaultTimeout, CancellationToken cancellationToken = default)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(body ?? Array.Empty<byte>()),
                downloadHandler = new DownloadHandlerBuffer()
            };
            if (!string.IsNullOrEmpty(contentType))
                request.SetRequestHeader("Content-Type", contentType);
            return SendAsync(request, headers, timeout, cancellationToken);
        }

        /// <summary>发起 DELETE 请求。</summary>
        public static UniTask<HttpResponse> DeleteAsync(string url, Dictionary<string, string> headers = null, float timeout = DefaultTimeout, CancellationToken cancellationToken = default)
            => SendAsync(UnityWebRequest.Delete(url), headers, timeout, cancellationToken);

        /// <summary>发起 GET 并反序列化 JSON 响应。请求失败时返回 default(T) 并记录错误日志。</summary>
        public static async UniTask<T> GetJsonAsync<T>(string url, Dictionary<string, string> headers = null, float timeout = DefaultTimeout, CancellationToken cancellationToken = default)
        {
            var response = await GetAsync(url, headers, timeout, cancellationToken);
            if (!response.IsSuccess)
            {
                LogCore.Error(nameof(HttpCore), $"GET {url} 失败: {response.ErrorMsg}");
                return default;
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(response.Text);
            }
            catch (Exception e)
            {
                LogCore.Error(nameof(HttpCore), $"GET {url} JSON 解析失败: {e.Message}");
                return default;
            }
        }

        /// <summary>
        /// 发送 UnityWebRequest 并等待结果。
        /// 统一处理：请求头、超时、取消、并发限制、结果判定与资源释放。
        /// 取消时主动 Abort 请求并释放并发槽，返回 Error(0, "请求已取消")。
        /// </summary>
        private static async UniTask<HttpResponse> SendAsync(UnityWebRequest request, Dictionary<string, string> headers, float timeout, CancellationToken cancellationToken)
        {
            if (headers != null)
            {
                foreach (var kvp in headers)
                    request.SetRequestHeader(kvp.Key, kvp.Value);
            }
            request.timeout = Math.Max(1, (int)timeout);

            // 排队阶段被取消：请求未发送（无需释放并发槽），但 request 的 native 资源必须释放
            try
            {
                await _throttle.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                request.Dispose();
                return HttpResponse.Error(0, "请求已取消");
            }

            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

                if (request.result == UnityWebRequest.Result.Success)
                    return HttpResponse.Success(request.responseCode, request.downloadHandler?.text);

                return HttpResponse.Error(request.responseCode, request.error, request.downloadHandler?.text);
            }
            catch (OperationCanceledException)
            {
                // 取消时中止底层连接，避免请求继续占用网络资源
                request.Abort();
                return HttpResponse.Error(0, "请求已取消");
            }
            catch (Exception e)
            {
                return HttpResponse.FailException(e);
            }
            finally
            {
                request.Dispose();
                _throttle.Release();
            }
        }
    }
}
