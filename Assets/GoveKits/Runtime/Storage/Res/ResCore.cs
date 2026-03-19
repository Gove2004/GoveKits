
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage.Res
{
    /// <summary>
    /// 资源加载类型枚举。
    /// </summary>
    public enum ResLoadType
    {
        /// <summary>
        /// Unity Resources。
        /// </summary>
        Resources = 1,

        /// <summary>
        /// AssetBundle。
        /// </summary>
        AssetBundle = 1 << 1,

        /// <summary>
        /// Addressables。
        /// </summary>
        Addressable = 1 << 2
    }

    /// <summary>
    /// 资源系统核心类。
    /// </summary>
    /// <remarks>
    /// 统一管理多加载源（Resources/AssetBundle/Addressables）并维护引用计数缓存。
    /// </remarks>
    public static class ResCore
    {
        private sealed class ResCacheEntry : RefCache
        {
            public UnityEngine.Object Asset;
            public ResLoadType LoadType;
        }

        private static readonly Dictionary<ResLoadType, IResLoader> Loaders = new()
        {
            { ResLoadType.Resources, new ResourcesResLoader() },
            { ResLoadType.AssetBundle, new AssetBundleResLoader() },
            { ResLoadType.Addressable, new AddressablesResLoader() },
        };
        private static readonly CacheContainer<ResCacheEntry> Cache = new();

        static ResCore()
        {
            Cache.OnCacheEmpty += (_, entry) =>
            {
                if (entry?.Asset == null)
                {
                    return;
                }

                Loaders[entry.LoadType].Unload(entry.Asset);
            };
        }

        /// <summary>
        /// 同步加载资源。
        /// </summary>
        /// <param name="loadType">加载类型。</param>
        /// <param name="path">资源路径。</param>
        /// <param name="useCache">是否使用缓存。</param>
        public static T Load<T>(ResLoadType loadType, string path, bool useCache = true) where T : UnityEngine.Object
        {
            string cacheKey = GetCacheKey<T>(loadType, path);
            if (useCache && Cache.TryGet(cacheKey, out ResCacheEntry cached))
            {
                return cached.Asset as T;
            }

            T asset = Loaders[loadType].Load<T>(path);
            if (asset != null && useCache)
            {
                Cache.Add(cacheKey, new ResCacheEntry
                {
                    Asset = asset,
                    RefCount = 1,
                    LoadType = loadType,
                });
            }

            return asset;
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="loadType">加载类型。</param>
        /// <param name="path">资源路径。</param>
        /// <param name="useCache">是否使用缓存。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public static async UniTask<T> LoadAsync<T>(ResLoadType loadType, string path, bool useCache = true, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            string cacheKey = GetCacheKey<T>(loadType, path);
            if (useCache && Cache.TryGet(cacheKey, out ResCacheEntry cached))
            {
                return cached.Asset as T;
            }

            T asset = await Loaders[loadType].LoadAsync<T>(path, cancellationToken);
            if (asset != null && useCache)
            {
                Cache.Add(cacheKey, new ResCacheEntry
                {
                    Asset = asset,
                    RefCount = 1,
                    LoadType = loadType,
                });
            }

            return asset;
        }

        /// <summary>
        /// 释放指定路径资源（减少引用计数，计数归零时执行卸载）。
        /// </summary>
        /// <param name="loadType">加载类型。</param>
        /// <param name="path">资源路径。</param>
        public static void Release<T>(ResLoadType loadType, string path) where T : UnityEngine.Object
        {
            string cacheKey = GetCacheKey<T>(loadType, path);
            Cache.TryRemove(cacheKey, out _);
        }

        private static string GetCacheKey<T>(ResLoadType loadType, string path)
        {
            // 将加载源、类型和路径合并，避免同路径不同来源的缓存冲突。
            return $"{loadType}:{typeof(T).FullName}:{path}";
        }
    }
}