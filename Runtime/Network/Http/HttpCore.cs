using System.Threading;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HTTP 网络模块的门面类。
    /// 封装请求构建器工厂、内置缓存、并发节流以及底层引擎的访问入口。
    /// 所有 HTTP 请求都应通过此类提供的 Get/Post/Put/Delete 方法发起。
    /// </summary>
    public static class HttpCore
    {
        private static readonly HttpCache _cache = new();
        private static readonly SemaphoreSlim _throttle = new(5);
        private static HttpEngine _engine;

        /// <summary>
        /// 初始化 HTTP 核心引擎及内部缓存、节流组件。
        /// 必须在首次发起任何 HTTP 请求之前调用。
        /// </summary>
        public static void Setup()
        {
            _engine = new HttpEngine(_cache, _throttle);
        }

        /// <summary>
        /// 内部 HTTP 引擎实例，供 HttpRequestBuilder 在执行请求时调用。
        /// </summary>
        internal static HttpEngine Engine { get; private set; }

        /// <summary>
        /// 内置的 HTTP 响应缓存实例，供引擎内部读写使用。
        /// </summary>
        internal static HttpCache Cache => _cache;

        /// <summary>
        /// 控制并发请求数量的节流信号量，默认最大并发数为 5。
        /// </summary>
        internal static SemaphoreSlim Throttle => _throttle;

        /// <summary>
        /// 创建 GET 请求构建器，指定目标 URL。
        /// </summary>
        /// <param name="url">请求的目标地址。</param>
        /// <returns>配置用的 HttpRequestBuilder 实例。</returns>
        public static HttpRequestBuilder Get(string url) => new HttpRequestBuilder(HttpMethod.GET, url);

        /// <summary>
        /// 创建 POST 请求构建器，指定目标 URL。
        /// </summary>
        /// <param name="url">请求的目标地址。</param>
        /// <returns>配置用的 HttpRequestBuilder 实例。</returns>
        public static HttpRequestBuilder Post(string url) => new HttpRequestBuilder(HttpMethod.POST, url);

        /// <summary>
        /// 创建 PUT 请求构建器，指定目标 URL。
        /// </summary>
        /// <param name="url">请求的目标地址。</param>
        /// <returns>配置用的 HttpRequestBuilder 实例。</returns>
        public static HttpRequestBuilder Put(string url) => new HttpRequestBuilder(HttpMethod.PUT, url);

        /// <summary>
        /// 创建 DELETE 请求构建器，指定目标 URL。
        /// </summary>
        /// <param name="url">请求的目标地址。</param>
        /// <returns>配置用的 HttpRequestBuilder 实例。</returns>
        public static HttpRequestBuilder Delete(string url) => new HttpRequestBuilder(HttpMethod.DELETE, url);

        /// <summary>
        /// 关闭 HTTP 核心：清空响应缓存、释放节流信号量并将引擎置空。
        /// 在不再需要 HTTP 功能或应用退出时调用。
        /// </summary>
        public static void Close()
        {
            _cache.Clear();
            _throttle.Dispose();
            Engine = null;
        }
    }
}
