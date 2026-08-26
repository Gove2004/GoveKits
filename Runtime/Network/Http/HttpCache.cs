using System;
using System.Collections.Concurrent;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HTTP 响应内存缓存，基于 ConcurrentDictionary 实现线程安全存储。
    /// 默认 TTL 为 300 秒，过期条目在读操作时自动清除（懒清除策略）。
    /// </summary>
    internal class HttpCache
    {
        private const int CacheTtlSec = 300;
        private readonly ConcurrentDictionary<string, (string data, long expire)> _cache = new();

        /// <summary>
        /// 尝试从缓存中获取指定键的数据。若条目已过期则自动移除。
        /// </summary>
        /// <param name="key">缓存键（通常为完整请求 URL）。</param>
        /// <param name="data">命中时返回缓存的响应文本，未命中则为 null。</param>
        /// <returns>命中且未过期时返回 true，否则返回 false。</returns>
        public bool TryGet(string key, out string data)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (DateTime.UtcNow.Ticks < item.expire)
                {
                    data = item.data;
                    return true;
                }
                _cache.TryRemove(key, out _);
            }
            data = null;
            return false;
        }

        /// <summary>
        /// 将响应数据存入缓存，并自动设置 300 秒的过期时间。
        /// </summary>
        /// <param name="key">缓存键（通常为完整请求 URL）。</param>
        /// <param name="data">要缓存的响应文本。</param>
        public void Set(string key, string data)
        {
            long expire = DateTime.UtcNow.AddSeconds(CacheTtlSec).Ticks;
            _cache[key] = (data, expire);
        }

        /// <summary>
        /// 清空缓存中的所有条目。通常在 HTTP 核心关闭时调用。
        /// </summary>
        public void Clear() => _cache.Clear();
    }
}
