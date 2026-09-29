using System;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HTTP 响应的不可变结构体，封装状态码、响应文本、成功标志和错误信息。
    /// 通过静态工厂方法 Success/Error/FailException 创建实例。
    /// </summary>
    public readonly struct HttpResponse
    {
        /// <summary>
        /// 请求是否成功（状态码 2xx 或缓存命中时为 true）。
        /// </summary>
        public readonly bool IsSuccess;

        /// <summary>
        /// HTTP 响应状态码。
        /// </summary>
        public readonly long StatusCode;

        /// <summary>
        /// 错误信息，请求失败时包含具体描述，成功时为 null。
        /// </summary>
        public readonly string ErrorMsg;

        /// <summary>
        /// 响应体文本内容。
        /// </summary>
        public readonly string Text;

        private HttpResponse(bool success, long code, string error, string text)
        {
            IsSuccess = success;
            StatusCode = code;
            ErrorMsg = error;
            Text = text;
        }

        /// <summary>
        /// 创建成功的 HTTP 响应。
        /// </summary>
        /// <param name="statusCode">HTTP 状态码。</param>
        /// <param name="text">响应体文本。</param>
        internal static HttpResponse Success(long statusCode, string text)
            => new HttpResponse(true, statusCode, null, text);

        internal static HttpResponse Error(long statusCode, string error, string text = null)
            => new HttpResponse(false, statusCode, error, text);

        internal static HttpResponse FailException(Exception ex)
            => new HttpResponse(false, 0, ex.Message, null);
    }
}
