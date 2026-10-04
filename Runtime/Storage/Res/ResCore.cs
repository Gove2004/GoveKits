using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 资源加载模式，决定编辑器模拟和生产环境的运行方式。
    /// </summary>
    public enum ResLoadMode
    {
        /// <summary>编辑器中使用模拟模式，打包后自动切换为离线内置模式。</summary>
        AutoOfflineMode,
        /// <summary>编辑器中使用模拟模式，打包后自动切换为主机 CDN 模式。</summary>
        AutoHostMode
    }

    /// <summary>
    /// 资源管理核心，封装 YooAsset 包裹管理、资源加载/卸载、热更新工作流。
    /// </summary>
    public static class ResCore
    {
        private static readonly Dictionary<string, ResourcePackage> _packages = new();

        /// <summary>版本请求/清单更新等异步操作的超时时间（毫秒）。</summary>
        private const int OperationTimeoutMs = 30000;

        /// <summary>资源下载失败后的最大重试次数。</summary>
        private const int DownloadMaxRetry = 3;

        /// <summary>下载重试的基础退避时长（秒），第 n 次重试前等待 2^(n-1) 倍（1s/2s/4s）。</summary>
        private const float DownloadRetryBackoffSeconds = 1f;

        #region 包裹初始化

        private static async UniTask<bool> InitPackageInternal(PackageConfig config)
        {
            // YooAssets 全局初始化：幂等，ResCore 是框架内唯一入口，使用方无需自行调用
            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();

            var packageFound = YooAssets.TryGetPackage(config.PackageName, out ResourcePackage package);
            if (!packageFound)
                package = YooAssets.CreatePackage(config.PackageName);

            if (!_packages.ContainsKey(config.PackageName))
                _packages.Add(config.PackageName, package);

            EPlayMode ePlayMode = EPlayMode.CustomPlayMode;
            if (config.PlayMode == ResLoadMode.AutoOfflineMode)
            {
#if UNITY_EDITOR
                ePlayMode = EPlayMode.EditorSimulateMode;
#else
                ePlayMode = EPlayMode.OfflinePlayMode;
#endif
            }
            if (config.PlayMode == ResLoadMode.AutoHostMode)
            {
#if UNITY_EDITOR
                ePlayMode = EPlayMode.EditorSimulateMode;
#else
                ePlayMode = EPlayMode.HostPlayMode;
#endif
            }

            InitializePackageOperation initOperation = null;
            switch (ePlayMode)
            {
                case EPlayMode.EditorSimulateMode:
#if UNITY_EDITOR
                    var simulateBuildResult = EditorSimulateBuildInvoker.Build(config.PackageName, (int)EBundleType.VirtualAssetBundle);
                    var packageRoot = simulateBuildResult.PackageRootDirectory;
                    var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                    var editorParam = new EditorSimulateModeOptions { EditorFileSystemParameters = editorFileSystem };
                    initOperation = package.InitializePackageAsync(editorParam);
                    break;
#else
                    LogCore.Error(nameof(ResCore), "真实环境中不能使用 EditorSimulate 模式，已强制切换为 Offline 模式");
                    goto case EPlayMode.OfflinePlayMode;
#endif

                case EPlayMode.OfflinePlayMode:
                    var offlineFileSystem = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                    var offlineParam = new OfflinePlayModeOptions { BuiltinFileSystemParameters = offlineFileSystem };
                    initOperation = package.InitializePackageAsync(offlineParam);
                    break;

                case EPlayMode.HostPlayMode:
                    var buildinFileSystem = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
                    var remoteServices = new DefaultRemoteServices(config.CDN_URL, config.Fallback_URL);
                    var cacheFileSystem = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteServices);
                    var hostParam = new HostPlayModeOptions
                    {
                        BuiltinFileSystemParameters = buildinFileSystem,
                        CacheFileSystemParameters = cacheFileSystem
                    };
                    initOperation = package.InitializePackageAsync(hostParam);
                    break;
            }

            await initOperation;

            if (initOperation.Status != EOperationStatus.Succeeded)
            {
                LogCore.Error(nameof(ResCore), $"包裹 {config.PackageName} 初始化失败: {initOperation.Error}");
                return false;
            }

            // YooAsset 3.x 将「初始化文件系统」与「装载清单」拆为两个独立步骤：
            // 不装载清单则任何加载都报 "Active package manifest not found"。
            // 此处统一装载，保证 InitPackageAsync 返回后即可加载；
            // HostPlayMode 的热更流程（UpdatePackageInternal）随后会装载新版本清单，重复装载幂等
            var versionOp = package.RequestPackageVersionAsync();
            bool versionDone = await WaitUntilOrTimeout(() => versionOp.IsDone);
            if (!versionDone || versionOp.Status != EOperationStatus.Succeeded)
            {
                string error = versionDone ? versionOp.Error : $"请求版本超时（{OperationTimeoutMs / 1000f}s）";
                LogCore.Error(nameof(ResCore), $"包裹 {config.PackageName} 获取版本失败: {error}");
                return false;
            }

            var manifestOp = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(versionOp.PackageVersion, OperationTimeoutMs));
            bool manifestDone = await WaitUntilOrTimeout(() => manifestOp.IsDone);
            if (!manifestDone || manifestOp.Status != EOperationStatus.Succeeded)
            {
                string error = manifestDone ? manifestOp.Error : $"装载清单超时（{OperationTimeoutMs / 1000f}s）";
                LogCore.Error(nameof(ResCore), $"包裹 {config.PackageName} 装载清单失败: {error}");
                return false;
            }

            LogCore.Success(nameof(ResCore), $"包裹 {config.PackageName} 初始化成功（版本 {versionOp.PackageVersion}）");
            return true;
        }

        /// <summary>
        /// 初始化资源包裹。
        /// </summary>
        /// <param name="config">包裹配置信息。</param>
        /// <returns>初始化是否成功。</returns>
        public static async UniTask<bool> InitPackageAsync(PackageConfig config)
            => await InitPackageInternal(config);

        /// <summary>
        /// 执行完整的包裹初始化和热更新流程。
        /// </summary>
        /// <param name="config">包裹配置。</param>
        /// <param name="callbacks">热更新各阶段的回调通知。</param>
        /// <returns>整个工作流是否成功。</returns>
        public static async UniTask<bool> PackageWorkflowAsync(PackageConfig config, UpdateCallbacks callbacks)
        {
            bool init = await InitPackageInternal(config);
            if (!init) return false;
            return await UpdatePackageInternal(config.PackageName, callbacks);
        }

        #endregion

        #region 包裹管理

        /// <summary>
        /// 销毁指定资源包裹，将其从管理器中移除并调用 YooAsset 销毁接口。
        /// </summary>
        /// <param name="packageName">要销毁的包裹名称。</param>
        public static void DestroyPackage(string packageName)
            => DestroyPackageAsync(packageName).Forget();

        /// <summary>
        /// 异步销毁指定资源包裹：先等 DestroyPackageAsync 完成，再从 YooAssets 中移除（移除要求包裹已完成销毁）。
        /// </summary>
        /// <param name="packageName">要销毁的包裹名称。</param>
        public static async UniTask DestroyPackageAsync(string packageName)
        {
            if (!_packages.Remove(packageName)) return;
            var package = YooAssets.GetPackage(packageName);
            if (package == null) return;

            await package.DestroyPackageAsync();
            YooAssets.RemovePackage(packageName);
        }

        #endregion

        #region 路径解析

        /// <summary>
        /// 解析资源位置。location 必须为 "PackageName:AssetPath" 格式（YooAsset 3.x 对齐：无全局默认包裹概念）。
        /// </summary>
        private static (ResourcePackage pkg, string assetPath) ParseLocation(string location)
        {
            int colonIndex = location.IndexOf(':');

            if (colonIndex <= 0)
            {
                LogCore.Error(nameof(ResCore), $"location 缺少包名前缀，必须为 \"PackageName:AssetPath\" 格式: {location}");
                return (null, location);
            }

            string pkgName = location.Substring(0, colonIndex);
            string assetPath = location.Substring(colonIndex + 1);

            if (!_packages.TryGetValue(pkgName, out var pkg))
            {
                LogCore.Error(nameof(ResCore), $"尚未初始化包裹: {pkgName}，无法加载资源: {location}");
                return (null, assetPath);
            }

            return (pkg, assetPath);
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 异步加载指定位置的资源（泛型版本）。
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="location">资源位置，必须为 "PackageName:AssetPath" 格式。</param>
        /// <returns>资源加载句柄。</returns>
        public static AssetHandle LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadAssetAsync<T>(assetPath);
        }

        /// <summary>
        /// 异步加载指定位置的资源（非泛型版本）。
        /// </summary>
        /// <param name="location">资源位置。</param>
        /// <param name="type">期望的资源类型。</param>
        /// <returns>资源加载句柄。</returns>
        public static AssetHandle LoadAssetAsync(string location, Type type)
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadAssetAsync(assetPath, type);
        }

        /// <summary>
        /// 异步加载原始文件（YooAsset 3.x 中对应 BundleFileHandle，可通过 GetRawFileText/GetRawFileData 读取内容）。
        /// </summary>
        /// <param name="location">文件位置。</param>
        /// <returns>原始文件加载句柄。</returns>
        public static BundleFileHandle LoadRawFileAsync(string location)
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadBundleFileAsync(assetPath);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="location">场景位置。</param>
        /// <param name="mode">场景加载模式。</param>
        /// <param name="suspendLoad">是否暂停加载直到显式继续。</param>
        /// <returns>场景加载句柄。</returns>
        public static YooAsset.SceneHandle LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Single, bool suspendLoad = false)
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadSceneAsync(assetPath, mode, allowSceneActivation: !suspendLoad);
        }

        /// <summary>
        /// 加载资源并立即实例化为 GameObject。加载完成后自动释放句柄。
        /// </summary>
        /// <param name="location">资源位置。</param>
        /// <param name="parent">实例化后的父级 Transform。</param>
        /// <returns>实例化的 GameObject，失败时返回 null。</returns>
        public static async UniTask<GameObject> InstantiateAsync(string location, Transform parent = null)
        {
            var handle = LoadAssetAsync<GameObject>(location);
            if (handle == null) return null;

            await handle;
            if (handle.Status == EOperationStatus.Succeeded)
            {
                var go = handle.InstantiateSync(new InstantiateOptions(true, parent, true));
                Release(handle);
                return go;
            }

            LogCore.Error(nameof(ResCore), $"实例化失败: {location} Error: {handle.Error}");
            Release(handle);
            return null;
        }

        /// <summary>
        /// 同步加载指定位置的资源（泛型版本）。
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="location">资源位置。</param>
        /// <returns>资源加载句柄。</returns>
        public static AssetHandle LoadAssetSync<T>(string location) where T : UnityEngine.Object
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadAssetSync<T>(assetPath);
        }

        #endregion

        #region 内存管理

        /// <summary>
        /// 释放资源加载句柄。
        /// </summary>
        /// <param name="handle">要释放的句柄。</param>
        public static void Release(HandleBase handle)
            => handle?.Release();

        /// <summary>
        /// 卸载指定包裹中未被引用的资源。
        /// </summary>
        /// <param name="packageName">包裹名称。</param>
        public static void UnloadUnusedAssets(string packageName)
        {
            if (_packages.TryGetValue(packageName, out var pkg))
                pkg.UnloadUnusedAssetsAsync();
        }

        /// <summary>
        /// 清除指定包裹的全部下载缓存文件（ClearAllBundleFiles）并触发回调。
        /// 注意：这不是卸载/销毁包裹本身——销毁包裹走 <see cref="CloseAsync"/>，
        /// 本方法仅在需要释放磁盘缓存空间时使用。
        /// </summary>
        /// <param name="packageName">包裹名称。</param>
        /// <param name="onSuccess">清除成功回调。</param>
        /// <param name="onFailure">清除失败回调。</param>
        public static async UniTaskVoid ClearCacheFiles(string packageName, Action onSuccess = null, Action onFailure = null)
        {
            var package = YooAssets.GetPackage(packageName);
            if (package == null) { onFailure?.Invoke(); return; }

            var operation = package.ClearCacheAsync(new ClearCacheOptions(ClearCacheMethods.ClearAllBundleFiles));
            await operation;

            if (operation.Status == EOperationStatus.Succeeded)
                onSuccess?.Invoke();
            else
                onFailure?.Invoke();
        }

        #endregion

        #region 热更新内部方法

        /// <summary>
        /// 等待条件满足或超时。返回 true 表示条件满足，false 表示超时。
        /// 注意：YooAsset 未公开 operation 的取消 API（AbortOperation 为 internal），
        /// 超时后底层 operation 仍会在后台运行至自然完成，仅其结果被忽略，上层可安全重试。
        /// </summary>
        private static async UniTask<bool> WaitUntilOrTimeout(Func<bool> condition, int timeoutMs = OperationTimeoutMs)
        {
            float deadline = Time.realtimeSinceStartup + timeoutMs / 1000f;
            while (!condition())
            {
                if (Time.realtimeSinceStartup >= deadline) return false;
                await UniTask.Yield();
            }
            return true;
        }

        private static async UniTask<bool> UpdatePackageInternal(string packageName, UpdateCallbacks callbacks)
        {
            if (!_packages.TryGetValue(packageName, out var pkg))
            {
                LogCore.Error(nameof(ResCore), $"热更失败：找不到包裹 {packageName}");
                return false;
            }

            LogCore.Success(nameof(ResCore), $"开始更新：{packageName}");
            callbacks?.OnCheckVersionBegin?.Invoke();

            var versionOp = pkg.RequestPackageVersionAsync();
            bool versionDone = await WaitUntilOrTimeout(() => versionOp.IsDone);

            if (!versionDone || versionOp.Status != EOperationStatus.Succeeded)
            {
                string error = versionDone ? versionOp.Error : $"请求版本超时（{OperationTimeoutMs / 1000f}s）";
                callbacks?.OnCheckVersionFailed?.Invoke(error);
                LogCore.Error(nameof(ResCore), $"获取版本失败：{error}");
                return false;
            }

            string latestVersion = versionOp.PackageVersion;
            callbacks?.OnCheckVersionSuccess?.Invoke(latestVersion);
            LogCore.Success(nameof(ResCore), $"获取最新版本：{latestVersion}");

            callbacks?.OnUpdateManifestBegin?.Invoke();
            var manifestOp = pkg.LoadPackageManifestAsync(new LoadPackageManifestOptions(latestVersion, OperationTimeoutMs));
            bool manifestDone = await WaitUntilOrTimeout(() => manifestOp.IsDone);

            if (!manifestDone || manifestOp.Status != EOperationStatus.Succeeded)
            {
                string error = manifestDone ? manifestOp.Error : $"更新清单超时（{OperationTimeoutMs / 1000f}s）";
                callbacks?.OnUpdateManifestFailed?.Invoke(error);
                LogCore.Error(nameof(ResCore), $"更新清单失败：{error}");
                return false;
            }
            callbacks?.OnUpdateManifestSuccess?.Invoke();
            LogCore.Success(nameof(ResCore), $"更新清单成功");

            var downloader = pkg.CreateResourceDownloader(new ResourceDownloaderOptions(10, 3));
            if (downloader.TotalDownloadCount == 0)
            {
                callbacks?.OnDownloadFinish?.Invoke(new DownloadCompletedEventArgs(packageName, true, null));
                return true;
            }

            int attempt = 0;
            while (true)
            {
                callbacks?.OnDownloadBegin?.Invoke(downloader.TotalDownloadCount, downloader.TotalDownloadBytes);
                downloader.DownloadFileStarted += (args) => callbacks?.OnDownloadFileBegin?.Invoke(args);
                downloader.DownloadError += (args) => callbacks?.OnDownloadError?.Invoke(args);
                downloader.DownloadProgressChanged += (args) => callbacks?.OnDownloadUpdate?.Invoke(args);
                downloader.DownloadCompleted += (args) => callbacks?.OnDownloadFinish?.Invoke(args);

                downloader.StartDownload();
                await UniTask.WaitUntil(() => downloader.IsDone);

                if (downloader.Status == EOperationStatus.Succeeded)
                    return true;

                attempt++;
                LogCore.Error(nameof(ResCore), $"下载资源失败（第 {attempt}/{DownloadMaxRetry} 次）: {downloader.Error}");
                // 下载器失败后不可复用，重建下载器重试；完成后 YooAsset 会自动触发 OnDownloadFinish(Succeeded=false)
                if (attempt >= DownloadMaxRetry) break;

                // 指数退避后再重试（1s/2s/4s），避免对刚失败的 CDN 立即连环施压
                float backoffSeconds = DownloadRetryBackoffSeconds * Mathf.Pow(2f, attempt - 1);
                LogCore.Warning(nameof(ResCore), $"{backoffSeconds:0.#}s 后重试下载…");
                await UniTask.Delay(TimeSpan.FromSeconds(backoffSeconds));

                downloader = pkg.CreateResourceDownloader(new ResourceDownloaderOptions(10, 3));
                if (downloader.TotalDownloadCount == 0)
                {
                    // 重试后无待下载内容 = 已全部就绪，需与首次路径一致补发成功回调
                    callbacks?.OnDownloadFinish?.Invoke(new DownloadCompletedEventArgs(packageName, true, null));
                    return true;
                }
            }

            LogCore.Error(nameof(ResCore), $"下载资源流程异常终止: {downloader.Error}");
            return false;
        }

        #endregion

        /// <summary>
        /// 清空所有已初始化的资源包裹。
        /// </summary>
        public static void Close()
            => CloseAsync().Forget();

        /// <summary>
        /// 异步关闭：等待每个包裹 DestroyPackageAsync 完成后从 YooAssets 移除（移除要求包裹已完成销毁），
        /// 保证 Close 后可重新 Setup 初始化同名包裹。
        /// </summary>
        public static async UniTask CloseAsync()
        {
            // 先摘除注册表再逐个销毁：await 期间若外部重新 Init/Destroy 不会破坏遍历，
            // 且保证无论销毁是否中断，注册表都已归位
            var packages = _packages.ToList();
            _packages.Clear();

            foreach (var kvp in packages)
            {
                await kvp.Value.DestroyPackageAsync();
                YooAssets.RemovePackage(kvp.Key);
            }
        }

        #region 内部辅助类

        private class DefaultRemoteServices : IRemoteService
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public DefaultRemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }

            /// <summary>返回按优先级排序的候选地址列表（主 CDN 优先，备用 CDN 兜底），YooAsset 依次尝试。</summary>
            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                var urls = new List<string>(2);
                if (!string.IsNullOrEmpty(_defaultHostServer))
                    urls.Add($"{_defaultHostServer}/{fileName}");
                if (!string.IsNullOrEmpty(_fallbackHostServer))
                    urls.Add($"{_fallbackHostServer}/{fileName}");
                if (urls.Count == 0)
                    urls.Add(fileName); // 双 CDN 均未配置时退化为相对路径（至少包含一个 URL 的接口契约）
                return urls;
            }
        }

        #endregion
    }
}
