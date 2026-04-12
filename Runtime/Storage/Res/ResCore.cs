using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core; // 你的日志库
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// YooAsset 2.3.18 资源管理核心代理类
    /// 完全适配 V2.2+ 最新的 FileSystem(虚拟文件系统) 底层架构
    /// 完全对齐 DownloaderOperation 所有的 Delegate 结构体
    /// </summary>
    public static class ResCore
    {
        #region 私有字段

        private static readonly Dictionary<string, ResourcePackage> _packages = new();
        private static string _defaultPackageName = "DefaultPackage";

        #endregion

        #region 核心初始化 (完全重构：适配 2.2+ FileSystem)
        
        public static async UniTask<bool> InitPackageAsync(PackageConfig config, bool setAsDefault = false)
        {
            var package = YooAssets.TryGetPackage(config.PackageName);
            if (package == null)
            {
                package = YooAssets.CreatePackage(config.PackageName);
            }

            if (!_packages.ContainsKey(config.PackageName))
            {
                _packages.Add(config.PackageName, package);
            }

            if (setAsDefault || _packages.Count == 1)
            {
                SetDefaultPackage(config.PackageName);
            }

            InitializationOperation initOperation = null;

            // YooAsset 2.2+ 必须使用 FileSystemParameters 进行初始化
            switch (config.PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
#if UNITY_EDITOR
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(config.PackageName);
                    var packageRoot = simulateBuildResult.PackageRootDirectory;
                    // 1. 创建编辑器文件系统
                    var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                    var editorParam = new EditorSimulateModeParameters();
                    editorParam.EditorFileSystemParameters = editorFileSystem;
                    initOperation = package.InitializeAsync(editorParam);
                    break;
#else
                    LogCore.Error(nameof(ResCore), "真实环境中不能使用 EditorSimulate 模式，已强制切换为 Offline 模式");
                    goto case EPlayMode.OfflinePlayMode;
#endif

                case EPlayMode.OfflinePlayMode:
                    // 1. 创建内置文件系统
                    var offlineFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    var offlineParam = new OfflinePlayModeParameters();
                    offlineParam.BuildinFileSystemParameters = offlineFileSystem;
                    initOperation = package.InitializeAsync(offlineParam);
                    break;

                case EPlayMode.HostPlayMode:
                    // 1. 创建内置文件系统
                    var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    // 2. 创建远端服务类
                    var remoteServices = new DefaultRemoteServices(config.CDN_URL, config.Fallback_URL);
                    // 3. 创建缓存文件系统
                    var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                    
                    var hostParam = new HostPlayModeParameters();
                    hostParam.BuildinFileSystemParameters = buildinFileSystem;
                    hostParam.CacheFileSystemParameters = cacheFileSystem;
                    initOperation = package.InitializeAsync(hostParam);
                    break;
            }

            await initOperation.Task;
            
            if (initOperation.Status != EOperationStatus.Succeed)
            {
                LogCore.Error(nameof(ResCore), $"包裹 {config.PackageName} 初始化失败: {initOperation.Error}");
                return false;
            }

            LogCore.Success(nameof(ResCore), $"包裹 {config.PackageName} 初始化成功");
            return true;
        }

        public static void SetDefaultPackage(string packageName)
        {
            if (!_packages.TryGetValue(packageName, out var pkg))
            {
                LogCore.Error(nameof(ResCore), $"找不到包裹: {packageName}，设置默认包裹失败！");
                return;
            }

            _defaultPackageName = packageName;
            YooAssets.SetDefaultPackage(pkg);
        }
        
        #endregion

        #region 路径解析逻辑 (语法糖)
        
        private static (ResourcePackage pkg, string assetPath) ParseLocation(string location)
        {
            int colonIndex = location.IndexOf(':');
            string pkgName;
            string assetPath;

            if (colonIndex > 0)
            {
                pkgName = location.Substring(0, colonIndex);
                assetPath = location.Substring(colonIndex + 1);
            }
            else
            {
                pkgName = _defaultPackageName;
                assetPath = location;
            }

            if (!_packages.TryGetValue(pkgName, out var pkg))
            {
                LogCore.Error(nameof(ResCore), $"尚未初始化包裹: {pkgName}，无法加载资源: {location}");
                return (null, assetPath);
            }

            return (pkg, assetPath);
        }
        
        #endregion

        #region 资源加载
        
        // ----------------- 异步加载 -----------------
        public static AssetHandle LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadAssetAsync<T>(assetPath);
        }

        public static AssetHandle LoadAssetAsync(string location, Type type)
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadAssetAsync(assetPath, type);
        }

        public static RawFileHandle LoadRawFileAsync(string location)
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadRawFileAsync(assetPath);
        }

        public static SceneHandle LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Single, bool suspendLoad = false)
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadSceneAsync(assetPath, mode, suspendLoad: suspendLoad);
        }

        public static async UniTask<GameObject> InstantiateAsync(string location, Transform parent = null)
        {
            var handle = LoadAssetAsync<GameObject>(location);
            if (handle == null) return null;

            await handle.Task;
            if (handle.Status == EOperationStatus.Succeed)
            {
                return handle.InstantiateSync(parent);
            }
            
            LogCore.Error(nameof(ResCore), $"实例化失败: {location} Error: {handle.LastError}");
            return null;
        }

        // ----------------- 同步加载 -----------------
        public static AssetHandle LoadAssetSync<T>(string location) where T : UnityEngine.Object
        {
            var (pkg, assetPath) = ParseLocation(location);
            return pkg?.LoadAssetSync<T>(assetPath);
        }
        
        #endregion

        #region 内存管理与卸载
        
        public static void Release(HandleBase handle)
        {
            handle?.Release();
        }

        public static void UnloadUnusedAssets(string packageName = null)
        {
            string pkgName = string.IsNullOrEmpty(packageName) ? _defaultPackageName : packageName;
            if (_packages.TryGetValue(pkgName, out var pkg))
            {
                pkg.UnloadUnusedAssetsAsync();
            }
        }

        public static void DestroyPackage(string packageName)
        {
            if (_packages.Remove(packageName))
            {
                var package = YooAssets.GetPackage(packageName);
                if (package != null)
                {
                    package.DestroyAsync(); 
                    YooAssets.RemovePackage(packageName);
                }
            }
        }

        public static async Task UnloadPackage(string packageName, Action onSuccess = null, Action onFailure = null)
        {
            var package = YooAssets.GetPackage(packageName);
            var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
            await operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onFailure?.Invoke();
            }
        }
        
        #endregion

        #region 一键热更新工作流
        
        public static async UniTask<bool> UpdatePackageWorkflowAsync(string packageName, UpdateCallbacks callbacks)
        {
            if (!_packages.TryGetValue(packageName, out var pkg))
            {
                LogCore.Error(nameof(ResCore), $"热更失败：找不到包裹 {packageName}");
                return false;
            }

            // ================= 1. 获取最新包裹版本 =================
            callbacks?.OnCheckVersionBegin?.Invoke();
            var versionOp = pkg.RequestPackageVersionAsync(); 
            await versionOp.Task;

            if (versionOp.Status != EOperationStatus.Succeed)
            {
                callbacks?.OnCheckVersionFailed?.Invoke(versionOp.Error);
                return false;
            }
            
            string latestVersion = versionOp.PackageVersion;
            callbacks?.OnCheckVersionSuccess?.Invoke(latestVersion);

            // ================= 2. 更新清单 Manifest =================
            callbacks?.OnUpdateManifestBegin?.Invoke();
            var manifestOp = pkg.UpdatePackageManifestAsync(latestVersion);
            await manifestOp.Task;

            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                callbacks?.OnUpdateManifestFailed?.Invoke(manifestOp.Error);
                return false;
            }
            callbacks?.OnUpdateManifestSuccess?.Invoke();

            // ================= 3. 创建下载器 =================
            var downloader = pkg.CreateResourceDownloader(10, 3);
            
            if (downloader.TotalDownloadCount == 0)
            {
                // 如果没有需要下载的，手动触发 Finish 回调通知外部
                callbacks?.OnDownloadFinish?.Invoke(new DownloaderFinishData 
                { 
                    PackageName = packageName, 
                    Succeed = true 
                });
                return true; 
            }

            // ================= 4. 绑定下载回调 (终极对齐源码结构) =================
            callbacks?.OnDownloadBegin?.Invoke(downloader.TotalDownloadCount, downloader.TotalDownloadBytes);

            // 对应源码：public delegate void DownloadFileBegin(DownloadFileData data);
            downloader.DownloadFileBeginCallback = (data) =>
            {
                callbacks?.OnDownloadFileBegin?.Invoke(data);
            };

            // 对应源码：public delegate void DownloadError(DownloadErrorData data);
            downloader.DownloadErrorCallback = (data) => 
            {
                callbacks?.OnDownloadError?.Invoke(data);
            };

            // 对应源码：public delegate void DownloadUpdate(DownloadUpdateData data);
            downloader.DownloadUpdateCallback = (data) => 
            {
                callbacks?.OnDownloadUpdate?.Invoke(data);
            };

            // 对应源码：public delegate void DownloaderFinish(DownloaderFinishData data);
            downloader.DownloadFinishCallback = (data) =>
            {
                callbacks?.OnDownloadFinish?.Invoke(data);
            };

            // ================= 5. 开始下载 =================
            downloader.BeginDownload();
            await downloader.Task;

            if (downloader.Status != EOperationStatus.Succeed)
            {
                LogCore.Error(nameof(ResCore), $"下载资源流程异常终止: {downloader.Error}");
                return false;
            }

            return true;
        }
        
        #endregion

        #region 内部辅助类 
        
        /// <summary>
        /// 联机模式必须的远端寻址服务
        /// </summary>
        private class DefaultRemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public DefaultRemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }
            
            public string GetRemoteMainURL(string fileName) => $"{_defaultHostServer}/{fileName}";
            public string GetRemoteFallbackURL(string fileName) => $"{_fallbackHostServer}/{fileName}";
        }
        
        #endregion
    }
}