using System.Collections.Generic;

namespace GoveKits.Runtime.Storage.Res
{
    public abstract class RefCache
    {
        public int RefCount = 0;
    }

    public class CacheContainer<T> where T : RefCache
    {
        private readonly Dictionary<string, T> _caches = new();
        public event System.Action<string, T> OnCacheMiss;  // 缓存未命中
        public event System.Action<string, T> OnCacheEmpty;  // 缓存被完全释放

        /// <summary>
        /// 新增缓存条目（增加引用计数）。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cache"></param>
        public void Add(string key, T cache)
        {
            if (cache.RefCount <= 0)
            {
                cache.RefCount = 1;
            }

            _caches[key] = cache;
        }

        /// <summary>
        /// 尝试获取缓存条目（并增加引用计数）。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool TryGet(string key, out T cache)
        {
            if (_caches.TryGetValue(key, out cache))
            {
                cache.RefCount++;
                return true;
            }

            OnCacheMiss?.Invoke(key, cache);
            return false;
        }

        /// <summary>
        /// 尝试卸载缓存条目（减少引用计数，计数归零时执行卸载）。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool TryRemove(string key, out T entry)
        {
            if (_caches.TryGetValue(key, out entry))
            {
                entry.RefCount--;
                if (entry.RefCount <= 0)
                {
                    _caches.Remove(key);
                    OnCacheEmpty?.Invoke(key, entry);
                }
                return true;
            }
            return false;
        }

        public IEnumerable<T> Values => _caches.Values;
    }
}