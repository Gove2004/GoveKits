using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage.Res
{
    /// <summary>
    /// 基于 AssetBundle 的资源加载器。
    /// </summary>
    /// <remarks>
    /// path 格式: "bundlePath|assetPath"。
    /// bundlePath 支持绝对路径，或相对 StreamingAssets 的路径。
    /// </remarks>
    public sealed class AssetBundleResLoader : IResLoader
    {
        private const char PathSeparator = '|';

        private readonly Dictionary<string, AssetBundle> bundles = new();
        private readonly Dictionary<string, int> bundleRefCounts = new();
        private readonly Dictionary<int, string> assetBundleMap = new();

        public ResLoadType LoadType => ResLoadType.AssetBundle;

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            SplitPath(path, out string bundlePath, out string assetPath);
            AssetBundle bundle = GetOrLoadBundle(bundlePath);
            T asset = bundle.LoadAsset<T>(assetPath);
            TrackAsset(asset, bundlePath);
            return asset;
        }

        public async UniTask<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            SplitPath(path, out string bundlePath, out string assetPath);
            AssetBundle bundle = await GetOrLoadBundleAsync(bundlePath, cancellationToken);

            AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetPath);
            await request.ToUniTask(cancellationToken: cancellationToken);

            T asset = request.asset as T;
            TrackAsset(asset, bundlePath);
            return asset;
        }

        public void Unload(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            int id = asset.GetInstanceID();
            if (!assetBundleMap.TryGetValue(id, out string bundlePath))
            {
                return;
            }

            assetBundleMap.Remove(id);

            if (bundleRefCounts.TryGetValue(bundlePath, out int count))
            {
                count--;
                if (count <= 0)
                {
                    // Bundle 引用归零后仅卸载 bundle 容器，不强制卸载已实例化对象。
                    bundleRefCounts.Remove(bundlePath);
                    if (bundles.TryGetValue(bundlePath, out AssetBundle bundle))
                    {
                        bundles.Remove(bundlePath);
                        bundle.Unload(unloadAllLoadedObjects: false);
                    }
                }
                else
                {
                    bundleRefCounts[bundlePath] = count;
                }
            }

            if (asset is GameObject || asset is Component)
            {
                return;
            }

            Resources.UnloadAsset(asset);
        }

        private void TrackAsset(UnityEngine.Object asset, string bundlePath)
        {
            if (asset == null)
            {
                return;
            }

            int id = asset.GetInstanceID();
            assetBundleMap[id] = bundlePath;
            if (bundleRefCounts.TryGetValue(bundlePath, out int count))
            {
                bundleRefCounts[bundlePath] = count + 1;
            }
            else
            {
                bundleRefCounts[bundlePath] = 1;
            }
        }

        private AssetBundle GetOrLoadBundle(string bundlePath)
        {
            if (bundles.TryGetValue(bundlePath, out AssetBundle loaded))
            {
                return loaded;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(NormalizeBundlePath(bundlePath));
            if (bundle == null)
            {
                throw new InvalidOperationException($"AssetBundle load failed: {bundlePath}");
            }

            bundles[bundlePath] = bundle;
            return bundle;
        }

        private async UniTask<AssetBundle> GetOrLoadBundleAsync(string bundlePath, CancellationToken cancellationToken)
        {
            if (bundles.TryGetValue(bundlePath, out AssetBundle loaded))
            {
                return loaded;
            }

            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(NormalizeBundlePath(bundlePath));
            await request.ToUniTask(cancellationToken: cancellationToken);

            AssetBundle bundle = request.assetBundle;
            if (bundle == null)
            {
                throw new InvalidOperationException($"AssetBundle async load failed: {bundlePath}");
            }

            bundles[bundlePath] = bundle;
            return bundle;
        }

        private static string NormalizeBundlePath(string bundlePath)
        {
            if (Path.IsPathRooted(bundlePath))
            {
                return bundlePath;
            }

            // 相对路径默认解释为 StreamingAssets 下路径。
            return Path.Combine(Application.streamingAssetsPath, bundlePath);
        }

        private static void SplitPath(string path, out string bundlePath, out string assetPath)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is empty.", nameof(path));
            }

            int index = path.IndexOf(PathSeparator);
            if (index <= 0 || index >= path.Length - 1)
            {
                throw new ArgumentException("AssetBundle path must be in format 'bundlePath|assetPath'.", nameof(path));
            }

            bundlePath = path.Substring(0, index).Trim();
            assetPath = path.Substring(index + 1).Trim();
        }
    }
}