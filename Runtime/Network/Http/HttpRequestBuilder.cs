using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HTTP 请求构建器，采用流式 API（Fluent API）风格配置请求参数。
    /// 支持设置请求头、查询参数、请求体、超时时间、重试次数和缓存策略。
    /// </summary>
    public class HttpRequestBuilder
    {
        /// <summary>
        /// 获取请求的 HTTP 方法。
        /// </summary>
        internal HttpMethod Method { get; }

        /// <summary>
        /// 获取请求的目标 URL。
        /// </summary>
        internal string Url { get; }

        /// <summary>
        /// 获取或设置请求头字典。
        /// </summary>
        internal Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// 获取或设置查询参数字典。
        /// </summary>
        internal Dictionary<string, string> QueryParams { get; private set; }

        /// <summary>
        /// 获取或设置请求体数据。
        /// </summary>
        internal object BodyData { get; private set; }

        /// <summary>
        /// 获取或设置请求超时时间（秒）。
        /// </summary>
        internal float Timeout { get; private set; } = 15f;

        /// <summary>
        /// 获取或设置最大重试次数。
        /// </summary>
        internal int RetryCount { get; private set; } = 3;

        /// <summary>
        /// 获取是否启用缓存。
        /// </summary>
        internal bool UseCache { get; private set; } = false;

        /// <summary>
        /// 创建 HTTP 请求构建器。
        /// </summary>
        /// <param name="method">HTTP 请求方法。</param>
        /// <param name="url">请求目标 URL。</param>
        public HttpRequestBuilder(HttpMethod method, string url)
        {
            Method = method;
            Url = url;
        }

        #region Fluent Setters

        /// <summary>
        /// 设置请求头。
        /// </summary>
        /// <param name="key">请求头键名。</param>
        /// <param name="value">请求头值。</param>
        /// <returns>当前构建器实例。</returns>
        public HttpRequestBuilder SetHeader(string key, string value) { Headers ??= new(); Headers[key] = value; return this; }

        /// <summary>
        /// 设置查询参数。
        /// </summary>
        /// <param name="key">查询参数键名。</param>
        /// <param name="value">查询参数值。</param>
        /// <returns>当前构建器实例。</returns>
        public HttpRequestBuilder SetQueryParam(string key, object value) { QueryParams ??= new(); QueryParams[key] = value.ToString(); return this; }

        /// <summary>
        /// 设置请求体数据，将在发送时自动序列化为 JSON。
        /// </summary>
        /// <param name="body">请求体对象。</param>
        /// <returns>当前构建器实例。</returns>
        public HttpRequestBuilder SetBody(object body) { BodyData = body; return this; }

        /// <summary>
        /// 设置请求超时时间（秒）。
        /// </summary>
        /// <param name="seconds">超时秒数。</param>
        /// <returns>当前构建器实例。</returns>
        public HttpRequestBuilder SetTimeout(float seconds) { Timeout = seconds; return this; }

        /// <summary>
        /// 设置最大重试次数。
        /// </summary>
        /// <param name="count">重试次数。</param>
        /// <returns>当前构建器实例。</returns>
        public HttpRequestBuilder SetRetry(int count) { RetryCount = count; return this; }

        /// <summary>
        /// 启用响应缓存，仅对 GET 请求生效。
        /// </summary>
        /// <returns>当前构建器实例。</returns>
        public HttpRequestBuilder EnableCache() { UseCache = true; return this; }

        #endregion

        /// <summary>
        /// 异步发送已配置的 HTTP 请求，返回 HttpResponse 结果。
        /// </summary>
        /// <param name="ct">可选的取消令牌，用于中止请求。</param>
        /// <returns>包含响应数据的 UniTask。</returns>
        public UniTask<HttpResponse> SendAsync(CancellationToken ct = default)
        {
            return HttpCore.Engine.ExecuteAsync(this, ct);
        }

        /// <summary>
        /// 异步发送 HTTP 请求并将 JSON 响应体自动反序列化为指定类型。
        /// </summary>
        /// <typeparam name="T">目标反序列化类型。</typeparam>
        /// <param name="ct">可选的取消令牌。</param>
        /// <returns>反序列化后的对象，请求失败时返回 default(T)。</returns>
        public async UniTask<T> GetJsonAsync<T>(CancellationToken ct = default)
        {
            var response = await SendAsync(ct);
            return response.IsSuccess ? response.GetJson<T>() : default;
        }
    }
}
