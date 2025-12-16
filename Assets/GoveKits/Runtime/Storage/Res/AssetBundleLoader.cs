using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks; // 核心依赖
using Object = UnityEngine.Object;


namespace GoveKits.Res
{
    /// <summary>
    /// 策略 C: AssetBundle (传统方案 - 逻辑最复杂)
    /// </summary>
    public class AssetBundleLoader : IResLoader
    {
        private AssetBundleManifest _manifest;
        private AssetBundle _mainBundle;
        // 缓存已加载的 Bundle 对象 (防止重复加载)
        private readonly Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();

        // 路径配置
        private string StreamingAssetsPath => Application.streamingAssetsPath + "/";
        private string MainManifestName
        {
            get
            {
#if UNITY_IOS
                return "IOS";
#elif UNITY_ANDROID
                return "Android";
#else
                return "PC"; // 请根据实际打包含义修改，如 "StreamingAssets"
#endif
            }
        }

        #region Path Helper
        // 假设路径格式: "BundleName/AssetName"
        private void ParsePath(string path, out string bundleName, out string assetName)
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash == -1)
            {
                bundleName = path.ToLower();
                assetName = path;
            }
            else
            {
                bundleName = path.Substring(0, lastSlash).ToLower();
                assetName = path.Substring(lastSlash + 1);
            }
        }
        #endregion

        #region Sync Implementation
        public T LoadSync<T>(string path) where T : Object
        {
            ParsePath(path, out string bundleName, out string assetName);

            // 1. 确保 Manifest 加载
            if (_manifest == null) LoadManifestSync();

            // 2. 加载依赖
            string[] deps = _manifest.GetAllDependencies(bundleName);
            foreach (var dep in deps) LoadBundleSync(dep);

            // 3. 加载目标 Bundle
            AssetBundle bundle = LoadBundleSync(bundleName);
            if (bundle == null) return null;

            // 4. 加载资源
            return bundle.LoadAsset<T>(assetName);
        }

        private void LoadManifestSync()
        {
            string uri = Path.Combine(StreamingAssetsPath, MainManifestName);
            _mainBundle = AssetBundle.LoadFromFile(uri);
            if (_mainBundle != null)
                _manifest = _mainBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        private AssetBundle LoadBundleSync(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out AssetBundle ab)) return ab;

            string uri = Path.Combine(StreamingAssetsPath, bundleName);
            ab = AssetBundle.LoadFromFile(uri);
            if (ab != null) _loadedBundles[bundleName] = ab;
            return ab;
        }
        #endregion

        #region Async Implementation (UniTask)
        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            ParsePath(path, out string bundleName, out string assetName);

            // 1. 确保 Manifest
            if (_manifest == null) await LoadManifestAsync();

            // 2. 加载依赖
            string[] deps = _manifest.GetAllDependencies(bundleName);
            foreach (var dep in deps) await LoadBundleAsync(dep);

            // 3. 加载目标 Bundle
            AssetBundle bundle = await LoadBundleAsync(bundleName);
            if (bundle == null) return null;

            // 4. 加载资源 (await AssetBundleRequest)
            var assetReq = await bundle.LoadAssetAsync<T>(assetName);
            
            return assetReq as T;
        }

        private async UniTask LoadManifestAsync()
        {
            string uri = Path.Combine(StreamingAssetsPath, MainManifestName);
            // AssetBundle.LoadFromFileAsync 可以直接被 UniTask await
            _mainBundle = await AssetBundle.LoadFromFileAsync(uri);
            if (_mainBundle != null)
                _manifest = _mainBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        private async UniTask<AssetBundle> LoadBundleAsync(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out AssetBundle ab)) return ab;

            string uri = Path.Combine(StreamingAssetsPath, bundleName);
            ab = await AssetBundle.LoadFromFileAsync(uri);
            
            // 线程安全检查 (防止并发同时加载同一个)
            if (ab != null && !_loadedBundles.ContainsKey(bundleName))
            {
                _loadedBundles[bundleName] = ab;
            }
            return ab;
        }
        #endregion

        public void Unload(Object asset)
        {
            // AB 模式下，ResManager 管理 Asset 引用，但 Bundle 的卸载需要 UnloadAllBundle
            // 单个 Asset 不需要操作，依靠 ForceClear 时统一卸载
        }

        public void UnloadAll()
        {
            foreach (var kvp in _loadedBundles)
            {
                if (kvp.Value != null) kvp.Value.Unload(true);
            }
            _loadedBundles.Clear();
            if (_mainBundle != null) _mainBundle.Unload(true);
            _mainBundle = null;
            _manifest = null;
            AssetBundle.UnloadAllAssetBundles(true);
        }
    }
}