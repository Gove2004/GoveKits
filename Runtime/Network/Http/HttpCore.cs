using System.Threading;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class HttpCore
    {
        internal static HttpCache Cache { get; } = new HttpCache();
        internal static SemaphoreSlim Throttle { get; private set; } = new SemaphoreSlim(5);

        // 工厂
        public static HttpRequestBuilder Get(string url) => new HttpRequestBuilder(HttpMethod.GET, url);
        public static HttpRequestBuilder Post(string url) => new HttpRequestBuilder(HttpMethod.POST, url);
        public static HttpRequestBuilder Put(string url) => new HttpRequestBuilder(HttpMethod.PUT, url);
        public static HttpRequestBuilder Delete(string url) => new HttpRequestBuilder(HttpMethod.DELETE, url);

        public static void OnShutdown()
        {
            Cache.Clear();
            Throttle?.Dispose();
        }
    }

}