
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    public class HttpRequestBuilder
    {
        internal HttpMethod Method { get; }
        internal string Url { get; }
        
        internal Dictionary<string, string> Headers { get; private set; }
        internal Dictionary<string, string> QueryParams { get; private set; }
        internal object BodyData { get; private set; }

        internal float Timeout { get; private set; } = 15f;
        internal int RetryCount { get; private set; } = 3;
        internal bool UseCache { get; private set; } = false;

        public HttpRequestBuilder(HttpMethod method, string url)
        {
            Method = method;
            Url = url;
        }

        #region Fluent Setters
        public HttpRequestBuilder SetHeader(string key, string value)
        {
            Headers ??= new Dictionary<string, string>();
            Headers[key] = value;
            return this;
        }

        public HttpRequestBuilder SetQueryParam(string key, object value)
        {
            QueryParams ??= new Dictionary<string, string>();
            QueryParams[key] = value?.ToString();
            return this;
        }

        public HttpRequestBuilder SetBody(object body) { BodyData = body; return this; }
        public HttpRequestBuilder SetTimeout(float seconds) { Timeout = seconds; return this; }
        public HttpRequestBuilder SetRetry(int count) { RetryCount = count; return this; }
        public HttpRequestBuilder EnableCache() { UseCache = true; return this; }
        #endregion

        #region 
        
        /// <summary>
        /// 发送请求
        /// </summary>
        public UniTask<HttpResponse> SendAsync(CancellationToken ct = default)
        {
            return HttpEngine.ExecuteAsync(this, ct);
        }

        /// <summary>
        /// 快捷，发送请求并获取JSON数据
        /// </summary>
        public async UniTask<T> GetJsonAsync<T>(CancellationToken ct = default)
        {
            var response = await SendAsync(ct);
            return response.IsSuccess ? response.GetJson<T>() : default;
        }
        #endregion
    }

}