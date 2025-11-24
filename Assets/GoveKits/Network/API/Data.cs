using System.Collections.Generic;


namespace GoveKits.Network
{
    /// <summary>
    /// 网络请求配置
    /// </summary>
    public class RequestData
    {
        // 请求路径
        public string endpoint;
        // 请求方法，默认 GET
        public HttpMethod method = HttpMethod.GET;
        // 请求体，string/byte[]/WWWForm
        public object body;
        // 请求头
        public Dictionary<string, string> headers;
        // 超时时间
        public float timeout;
        // 重试次数
        public int retryCount;
        // 是否使用缓存
        public bool useCache;
    }


    /// <summary>
    /// 网络响应
    /// </summary>
    public class ResponseData
    {
        /// 请求是否成功
        public bool success;
        // 状态码
        public long statusCode;
        // 错误信息
        public string error;
        // 响应文本
        public string text;
        // 响应字节数组
        public byte[] bytes;
        // 响应头
        public Dictionary<string, string> headers;
    }


    /// <summary>
    /// HTTP方法枚举
    /// </summary>
    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        DELETE,
        PATCH
    }
}