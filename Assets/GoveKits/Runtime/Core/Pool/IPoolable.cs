using UnityEngine;

namespace GoveKits.Runtime.Core.Pool
{
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
    }

    public static class PoolableExtensions
    {
        public static void ReturnToPool<T>(this T item) where T : class, IPoolable, new()
            => PoolCore.Return(item);

        public static void ReturnToPool(this GameObject item)
            => PoolCore.Return(item);
    }
}