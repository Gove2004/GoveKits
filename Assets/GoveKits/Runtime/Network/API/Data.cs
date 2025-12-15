using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json; // 必须引用
using UnityEngine.Networking;

namespace GoveKits.Network
{
    public enum HttpMethod { GET, POST, PUT, DELETE }

    /// <summary>
    /// 请求构建器
    /// </summary>
    public class RequestData
    {
        // --- 核心数据 ---
        public string Url;
        public HttpMethod Method;
        public Dictionary<string, string> Headers;
        
        // 底层数据 (构建时立即生成，避免运行时GC和计算)
        internal byte[] _bodyBytes;
        internal string _contentType;

        // --- 配置 (默认值) ---
        public float Timeout = WebAPI.CONST_TIMEOUT;
        public int Retry = WebAPI.CONST_RETRY;
        public bool UseCache = false;

        private RequestData() { }

        // ========================================================================
        // 静态工厂方法 (入口)
        // ========================================================================

        /// <summary>
        /// GET 请求 (支持 URL 参数自动拼接)
        /// </summary>
        public static RequestData Get(string url, Dictionary<string, string> queryParams = null)
        {
            return new RequestData 
            { 
                Url = BuildUrlWithQuery(url, queryParams), 
                Method = HttpMethod.GET 
            };
        }

        /// <summary>
        /// POST 请求 (自动序列化 Body 为 JSON)
        /// </summary>
        /// <param name="body">可以是 class, struct, 或 json string</param>
        public static RequestData Post(string url, object body)
        {
            return CreateWithBody(url, HttpMethod.POST, body);
        }

        /// <summary>
        /// PUT 请求 (通常用于更新数据)
        /// </summary>
        public static RequestData Put(string url, object body)
        {
            return CreateWithBody(url, HttpMethod.PUT, body);
        }

        /// <summary>
        /// DELETE 请求 (支持 URL 参数)
        /// </summary>
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
        
        public RequestData AddHeader(string key, string value)
        {
            if (Headers == null) Headers = new Dictionary<string, string>();
            Headers[key] = value;
            return this;
        }

        public RequestData SetTimeout(float seconds) { Timeout = seconds; return this; }
        public RequestData SetRetry(int count) { Retry = count; return this; }
        public RequestData SetCache(bool enable) { UseCache = enable; return this; }
    }

    /// <summary>
    /// 响应数据 (栈上结构体，零GC)
    /// </summary>
    public readonly struct ResponseData
    {
        public readonly bool Success;
        public readonly long StatusCode;
        public readonly string Error;
        public readonly string Text;
        public readonly byte[] Data;

        public ResponseData(bool success, long code, string error, string text, byte[] data)
        {
            Success = success;
            StatusCode = code;
            Error = error;
            Text = text;
            Data = data;
        }

        // 尝试将结果反序列化为对象 T
        public T As<T>()
        {
            if (!Success || string.IsNullOrEmpty(Text)) return default;
            try { return JsonConvert.DeserializeObject<T>(Text); }
            catch { return default; }
        }

        public static ResponseData Fail(string error, long code = 0) 
            => new ResponseData(false, code, error, null, null);
    }
}