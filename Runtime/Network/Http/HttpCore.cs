using System.Threading;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public class HttpCore : ICore
    {
        internal HttpCache Cache { get; } = new HttpCache();
        internal SemaphoreSlim Throttle { get; private set; } = new SemaphoreSlim(5);

        // 工厂
        public HttpRequestBuilder Get(string url) => new HttpRequestBuilder(this, HttpMethod.GET, url);
        public HttpRequestBuilder Post(string url) => new HttpRequestBuilder(this, HttpMethod.POST, url);
        public HttpRequestBuilder Put(string url) => new HttpRequestBuilder(this, HttpMethod.PUT, url);
        public HttpRequestBuilder Delete(string url) => new HttpRequestBuilder(this, HttpMethod.DELETE, url);

        public void ClearCache() => Cache.Clear();

        public void OnShutdown()
        {
            ClearCache();
            Throttle?.Dispose();
        }
    }

}