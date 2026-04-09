

using System;
using System.Collections.Concurrent;

namespace GoveKits.Runtime.Network
{
    internal class HttpCache
    {
        private const int CACHE_TTL_SEC = 300;
        private readonly ConcurrentDictionary<string, (string data, long expire)> _cache = new();

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

        public void Set(string key, string data)
        {
            long expire = DateTime.UtcNow.AddSeconds(CACHE_TTL_SEC).Ticks;
            _cache[key] = (data, expire);
        }

        public void Clear() => _cache.Clear();
    }
}