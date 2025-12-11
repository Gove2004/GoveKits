using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GoveKits; // 引入 UniTask
using Object = UnityEngine.Object;

namespace GoveKits.Res
{

    public static class ResManager
    {
        // 配置：默认加载模式 (用于 Load<T> 通用接口)
        public static ResType DefaultType { get; private set; } = ResType.Resources;

        // 缓存池：Key = 资源路径
        private static readonly Dictionary<string, AssetInfo> _assetCache = new Dictionary<string, AssetInfo>();

        // 加载器策略实例
        private static readonly ResourceLoader _resLoader = new ResourceLoader();
        private static readonly AddressableLoader _aaLoader = new AddressableLoader();
        private static readonly AssetBundleLoader _abLoader = new AssetBundleLoader();

        /// <summary>
        /// 初始化资源管理器 (设置默认加载模式)
        /// </summary>
        public static void Initialize(ResType defaultType)
        {
            DefaultType = defaultType;
            DebugLogger.Log("ResManager", $"Initialized. Default Mode: {DefaultType}");
        }

        // 获取对应的加载器
        private static IResLoader GetLoader(ResType type)
        {
            switch (type)
            {
                case ResType.Resources: return _resLoader;
                case ResType.Addressable: return _aaLoader;
                case ResType.AssetBundle: return _abLoader;
                default: return _resLoader;
            }
        }

        #region 同步加载 API

        /// <summary>
        /// 【通用】使用默认模式同步加载 (AudioManager 主要用这个)
        /// </summary>
        public static T Load<T>(string path) where T : Object
            => LoadInternalSync<T>(path, DefaultType);

        /// <summary>
        /// 强制从 Resources 加载
        /// </summary>
        public static T LoadFromResources<T>(string path) where T : Object 
            => LoadInternalSync<T>(path, ResType.Resources);

        /// <summary>
        /// 强制从 Addressables 加载 (同步等待)
        /// </summary>
        public static T LoadFromAA<T>(string path) where T : Object 
            => LoadInternalSync<T>(path, ResType.Addressable);

        /// <summary>
        /// 强制从 AssetBundle 加载
        /// </summary>
        public static T LoadFromAB<T>(string path) where T : Object 
            => LoadInternalSync<T>(path, ResType.AssetBundle);

        #endregion

        #region 异步加载 API (UniTask)

        /// <summary>
        /// 【通用】使用默认模式异步加载
        /// </summary>
        public static UniTask<T> LoadAsync<T>(string path) where T : Object
            => LoadInternalAsync<T>(path, DefaultType);

        public static UniTask<T> LoadAsyncFromResources<T>(string path) where T : Object
            => LoadInternalAsync<T>(path, ResType.Resources);

        public static UniTask<T> LoadAsyncFromAA<T>(string path) where T : Object
            => LoadInternalAsync<T>(path, ResType.Addressable);

        public static UniTask<T> LoadAsyncFromAB<T>(string path) where T : Object
            => LoadInternalAsync<T>(path, ResType.AssetBundle);

        #endregion

        #region 资源释放与清理

        /// <summary>
        /// 释放资源 (引用计数 -1)
        /// </summary>
        public static void Release(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            if (_assetCache.TryGetValue(path, out AssetInfo info))
            {
                info.RefCount--;
                // Debug.Log($"[Res] Release: {path}, Ref: {info.RefCount}");

                if (info.RefCount <= 0)
                {
                    // 引用归零，卸载
                    GetLoader(info.LoadType).Unload(info.Asset);
                    _assetCache.Remove(path);
                    // Debug.Log($"[Res] Unloaded: {path}");
                }
            }
        }

        /// <summary>
        /// 强制清空缓存 (场景切换时使用)
        /// </summary>
        public static void ClearAllCache()
        {
            foreach (var kvp in _assetCache)
            {
                if (kvp.Value.Asset != null)
                {
                    GetLoader(kvp.Value.LoadType).Unload(kvp.Value.Asset);
                }
            }
            _assetCache.Clear();
            
            // 如果使用了 AB 加载器，需要清理其内部 Bundle 缓存
            _abLoader.UnloadAll();

            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        #endregion

        #region 内部核心逻辑

        private static T LoadInternalSync<T>(string path, ResType type) where T : Object
        {
            if (string.IsNullOrEmpty(path)) return null;

            // 1. 检查缓存
            if (_assetCache.TryGetValue(path, out AssetInfo info))
            {
                if (info.Asset != null)
                {
                    info.RefCount++;
                    return info.Asset as T;
                }
                _assetCache.Remove(path);
            }

            // 2. 真实加载
            IResLoader loader = GetLoader(type);
            T asset = loader.LoadSync<T>(path);

            // 3. 加入缓存
            if (asset != null)
            {
                _assetCache.Add(path, new AssetInfo
                {
                    Asset = asset,
                    Path = path,
                    LoadType = type,
                    RefCount = 1
                });
            }
            else
            {
                DebugLogger.LogError("ResManager", $"Sync Load Failed: {path} via {type}");
            }

            return asset;
        }

        private static async UniTask<T> LoadInternalAsync<T>(string path, ResType type) where T : Object
        {
            if (string.IsNullOrEmpty(path)) return null;

            // 1. 检查缓存
            if (_assetCache.TryGetValue(path, out AssetInfo info))
            {
                if (info.Asset != null)
                {
                    info.RefCount++;
                    return info.Asset as T;
                }
                _assetCache.Remove(path);
            }

            // 2. 真实加载 (await UniTask)
            IResLoader loader = GetLoader(type);
            T asset = await loader.LoadAsync<T>(path);

            // 3. 加入缓存
            if (asset != null)
            {
                // 双重检查：防止 await 期间被其他地方同步加载了
                if (!_assetCache.ContainsKey(path))
                {
                    _assetCache.Add(path, new AssetInfo
                    {
                        Asset = asset,
                        Path = path,
                        LoadType = type,
                        RefCount = 1
                    });
                }
                else
                {
                    _assetCache[path].RefCount++;
                }
            }
            else
            {
                DebugLogger.LogError("ResManager", $"Async Load Failed: {path} via {type}");
            }

            return asset;
        }

        #endregion
    }
}