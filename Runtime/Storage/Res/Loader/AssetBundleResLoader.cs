using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 企业级 AssetBundle 加载器，内部自带 Bundle 引用计数和依赖追踪。
    /// </summary>
    public sealed class AssetBundleResLoader : IResLoader
    {
        private class BundleRecord
        {
            public AssetBundle Bundle;
            public int RefCount;
            public string[] Dependencies;
        }

        private readonly string _basePath;
        private AssetBundleManifest _manifest;
        
        // 缓存已经加载的 Bundle
        private readonly Dictionary<string, BundleRecord> _bundles = new();
        // 正在加载中的 Bundle 任务（防重入）
        private readonly Dictionary<string, UniTask<AssetBundle>> _loadingTasks = new();

        /// <summary>
        /// 提供给外部注入的委托：将资源路径映射到所在的 Bundle Name。
        /// 实际项目中，通常会在打 AB 包时生成一个 json 配置，游戏启动时读取。
        /// </summary>
        public Func<string, string> PathToBundleNameMapper { get; set; }

        public AssetBundleResLoader(string basePath, string manifestName)
        {
            _basePath = basePath;
            // 同步加载 Manifest（启动时必须）
            string manifestPath = Path.Combine(_basePath, manifestName);
            var mainBundle = AssetBundle.LoadFromFile(manifestPath);
            if (mainBundle != null)
            {
                _manifest = mainBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                mainBundle.Unload(false); // 取出 Manifest 后卸载自身包体
            }
            else
            {
                CoreLocator.Log.Error(nameof(AssetBundleResLoader), "Unable to load AssetBundleManifest");
            }
        }

        private string GetBundleName(string path)
        {
            return PathToBundleNameMapper?.Invoke(path) ?? path.ToLower();
        }

        #region IResLoader 接口实现

        public T Load<T>(string path) where T : Object
        {
            string bundleName = GetBundleName(path);
            var bundle = LoadBundleSync(bundleName);
            if (bundle == null) return null;

            // 这里不用卸载 Bundle，因为缓存起来了
            return bundle.LoadAsset<T>(path);
        }

        public async UniTask<T> LoadAsync<T>(string path, CancellationToken ct = default) where T : Object
        {
            string bundleName = GetBundleName(path);
            var bundle = await LoadBundleAsync(bundleName, ct);
            if (bundle == null) return null;

            var request = bundle.LoadAssetAsync<T>(path);
            await request.ToUniTask(cancellationToken: ct);
            return request.asset as T;
        }

        public void Unload(string path, Object asset)
        {
            // 当 ResCore 说这个 Asset 没用了，我们就给对应的 Bundle 扣减引用计数
            string bundleName = GetBundleName(path);
            ReleaseBundle(bundleName);
            
            // 注意：不要去 Destroy(asset)，Unity 从 AB 里拉出来的 Asset 会在 Bundle.Unload(true) 时自动清理
        }

        public void Clear()
        {
            foreach (var kvp in _bundles)
            {
                kvp.Value.Bundle?.Unload(true);
            }
            _bundles.Clear();
            _loadingTasks.Clear();
        }

        #endregion

        #region 内部 Bundle 生命周期管理

        private AssetBundle LoadBundleSync(string bundleName)
        {
            if (_bundles.TryGetValue(bundleName, out var record))
            {
                record.RefCount++;
                return record.Bundle;
            }

            // 1. 加载依赖包 (递归)
            string[] deps = _manifest.GetAllDependencies(bundleName);
            foreach (var dep in deps)
            {
                LoadBundleSync(dep); 
            }

            // 2. 加载自身包
            var bundle = AssetBundle.LoadFromFile(Path.Combine(_basePath, bundleName));
            
            _bundles[bundleName] = new BundleRecord
            {
                Bundle = bundle,
                RefCount = 1,
                Dependencies = deps
            };
            return bundle;
        }

        private async UniTask<AssetBundle> LoadBundleAsync(string bundleName, CancellationToken ct)
        {
            if (_bundles.TryGetValue(bundleName, out var record))
            {
                record.RefCount++;
                return record.Bundle;
            }

            // 防止并发加载同一个包报错
            if (_loadingTasks.TryGetValue(bundleName, out var task))
            {
                var result = await task;
                if (_bundles.TryGetValue(bundleName, out record)) record.RefCount++;
                return result;
            }

            var tcs = LoadBundleInternalAsync(bundleName, ct);
            _loadingTasks[bundleName] = tcs;

            try
            {
                return await tcs;
            }
            finally
            {
                _loadingTasks.Remove(bundleName);
            }
        }

        private async UniTask<AssetBundle> LoadBundleInternalAsync(string bundleName, CancellationToken ct)
        {
            string[] deps = _manifest.GetAllDependencies(bundleName);
            foreach (var dep in deps)
            {
                await LoadBundleAsync(dep, ct);
            }

            var request = AssetBundle.LoadFromFileAsync(Path.Combine(_basePath, bundleName));
            await request.ToUniTask(cancellationToken: ct);
            
            var bundle = request.assetBundle;
            _bundles[bundleName] = new BundleRecord
            {
                Bundle = bundle,
                RefCount = 1,
                Dependencies = deps
            };
            return bundle;
        }

        private void ReleaseBundle(string bundleName)
        {
            if (!_bundles.TryGetValue(bundleName, out var record)) return;

            record.RefCount--;
            if (record.RefCount <= 0)
            {
                // 1. 卸载依赖包
                foreach (var dep in record.Dependencies)
                {
                    ReleaseBundle(dep);
                }
                
                // 2. 卸载自身
                record.Bundle?.Unload(true);
                _bundles.Remove(bundleName);
            }
        }

        #endregion
    }
}