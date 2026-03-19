using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json; // 必须引用
using UnityEngine.Networking;

namespace GoveKits.Network
{
    /// <summary>
    /// HTTP 方法类型。
    /// </summary>
    public enum HttpMethod { GET, POST, PUT, DELETE }

    /// <summary>
    /// 请求构建器：以链式方式配置请求（方法、地址、头、超时、重试、缓存等），
    /// 并在构建阶段准备底层数据以减少运行时 GC。
    /// </summary>
    public class RequestData
    {
        // --- 核心数据 ---
        /// <summary>
        /// 最终请求地址（可为绝对地址，或已拼接查询参数的地址）。
        /// </summary>
        public string Url;
        /// <summary>
        /// HTTP 方法。
        /// </summary>
        public HttpMethod Method;
        /// <summary>
        /// 请求头字典（若同名键重复，后者覆盖前者）。
        /// </summary>
        public Dictionary<string, string> Headers;
        
        // 底层数据 (构建时立即生成，避免运行时GC和计算)
        internal byte[] _bodyBytes;
        internal string _contentType;

        // --- 配置 (默认值) ---
        /// <summary>
        /// 超时时间（秒）。默认取 <see cref="WebAPI.CONST_TIMEOUT"/>。
        /// </summary>
        public float Timeout = WebAPI.CONST_TIMEOUT;
        /// <summary>
        /// 最大重试次数（针对网络错误和 5xx）。默认取 <see cref="WebAPI.CONST_RETRY"/>。
        /// </summary>
        public int Retry = WebAPI.CONST_RETRY;
        /// <summary>
        /// 是否使用响应缓存（仅对 GET 有效）。默认 false。
        /// </summary>
        public bool UseCache = false;

        private RequestData() { }

        // ========================================================================
        // 静态工厂方法 (入口)
        // ========================================================================

        /// <summary>
        /// 构建一个 GET 请求（支持 URL 查询参数自动拼接）。
        /// </summary>
        /// <param name="url">基础地址或完整地址。</param>
        /// <param name="queryParams">可选的查询参数字典。</param>
        /// <returns>已配置的 <see cref="RequestData"/>。</returns>
        public static RequestData Get(string url, Dictionary<string, string> queryParams = null)
        {
            return new RequestData 
            { 
                Url = BuildUrlWithQuery(url, queryParams), 
                Method = HttpMethod.GET 
            };
        }

        /// <summary>
        /// 构建一个 POST 请求（自动将 body 序列化为 JSON）。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="body">可以是 class、struct 或 JSON 字符串。</param>
        /// <returns>已配置的 <see cref="RequestData"/>。</returns>
        public static RequestData Post(string url, object body)
        {
            return CreateWithBody(url, HttpMethod.POST, body);
        }

        /// <summary>
        /// 构建一个 PUT 请求（通常用于更新资源）。
        /// </summary>
        /// <param name="url">请求地址。</param>
        /// <param name="body">可以是 class、struct 或 JSON 字符串。</param>
        /// <returns>已配置的 <see cref="RequestData"/>。</returns>
        public static RequestData Put(string url, object body)
        {
            return CreateWithBody(url, HttpMethod.PUT, body);
        }

        /// <summary>
        /// 构建一个 DELETE 请求（支持 URL 查询参数）。
        /// </summary>
        /// <param name="url">基础地址或完整地址。</param>
        /// <param name="queryParams">可选的查询参数字典。</param>
        /// <returns>已配置的 <see cref="RequestData"/>。</returns>
        public static RequestData Delete(string url, Dictionary<string, string> queryParams = null)
        {
            return new RequestData 
            { 
                Url = BuildUrlWithQuery(url, queryParams), 
                Method = HttpMethod.DELETE 
            };
        }

        // ========================================================================
        // 内部辅助逻辑
        // ========================================================================

        private static RequestData CreateWithBody(string url, HttpMethod method, object body)
        {
            var req = new RequestData 
            { 
                Url = url, 
                Method = method,
                _contentType = "application/json"
            };

            if (body != null)
            {
                string json;
                // 如果已经是字符串，默认它是Json String；否则进行序列化
                if (body is string s) json = s;
                else json = JsonConvert.SerializeObject(body);

                req._bodyBytes = Encoding.UTF8.GetBytes(json);
            }
            return req;
        }

        private static string BuildUrlWithQuery(string url, Dictionary<string, string> queryParams)
        {
            if (queryParams == null || queryParams.Count == 0) return url;
            
            var sb = new StringBuilder(url);
            sb.Append(url.Contains("?") ? "&" : "?");
            
            int i = 0;
            foreach (var kv in queryParams)
            {
                if (i > 0) sb.Append("&");
                sb.Append($"{UnityWebRequest.EscapeURL(kv.Key)}={UnityWebRequest.EscapeURL(kv.Value)}");
                i++;
            }
            return sb.ToString();
        }

        // ========================================================================
        // 链式配置
        // ========================================================================
        
        /// <summary>
        /// 添加或覆盖一个请求头键值。
        /// </summary>
        /// <param name="key">请求头键。</param>
        /// <param name="value">请求头值。</param>
        /// <returns>当前 <see cref="RequestData"/>（便于链式调用）。</returns>
        public RequestData AddHeader(string key, string value)
        {
            if (Headers == null) Headers = new Dictionary<string, string>();
            Headers[key] = value;
            return this;
        }

        /// <summary>设置超时（秒）。</summary>
        public RequestData SetTimeout(float seconds) { Timeout = seconds; return this; }
        /// <summary>设置最大重试次数。</summary>
        public RequestData SetRetry(int count) { Retry = count; return this; }
        /// <summary>是否启用缓存（仅 GET 生效）。</summary>
        public RequestData SetCache(bool enable) { UseCache = enable; return this; }
    }

    /// <summary>
    /// 响应数据（readonly 结构体，尽量避免 GC）。
    /// </summary>
    public readonly struct ResponseData
    {
        /// <summary>请求是否成功。</summary>
        public readonly bool Success;
        /// <summary>HTTP 状态码。</summary>
        public readonly long StatusCode;
        /// <summary>错误信息（失败时）。</summary>
        public readonly string Error;
        /// <summary>响应文本（若为文本）。</summary>
        public readonly string Text;
        /// <summary>响应字节数据（若为二进制）。</summary>
        public readonly byte[] Data;

        /// <summary>
        /// 构造响应数据。
        /// </summary>
        /// <param name="success">是否成功。</param>
        /// <param name="code">HTTP 状态码。</param>
        /// <param name="error">错误信息。</param>
        /// <param name="text">响应文本。</param>
        /// <param name="data">响应二进制数据。</param>
        public ResponseData(bool success, long code, string error, string text, byte[] data)
        {
            Success = success;
            StatusCode = code;
            Error = error;
            Text = text;
            Data = data;
        }

        /// <summary>
        /// 将响应文本尝试反序列化为类型 <typeparamref name="T"/>。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns>反序列化结果或默认值。</returns>
        public T As<T>()
        {
            if (!Success || string.IsNullOrEmpty(Text)) return default;
            try { return JsonConvert.DeserializeObject<T>(Text); }
            catch { return default; }
        }

        /// <summary>
        /// 构建一个失败响应。
        /// </summary>
        /// <param name="error">错误信息。</param>
        /// <param name="code">可选状态码。</param>
        public static ResponseData Fail(string error, long code = 0) 
            => new ResponseData(false, code, error, null, null);
    }
}