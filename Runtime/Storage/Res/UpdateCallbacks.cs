using System;
using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 工业级热更回调集合
    /// </summary>
    public class UpdateCallbacks
    {
        // 阶段 1: 检查版本
        public Action OnCheckVersionBegin;
        public Action<string> OnCheckVersionSuccess; // 传入最新的版本号
        public Action<string> OnCheckVersionFailed;  // 传入错误信息

        // 阶段 2: 更新清单
        public Action OnUpdateManifestBegin;
        public Action OnUpdateManifestSuccess;
        public Action<string> OnUpdateManifestFailed;

        // 阶段 3: 资源下载
        public Action<int, long> OnDownloadBegin; // 需要下载的文件总数，总大小(字节)
        
        public Action<DownloadFileData> OnDownloadFileBegin; // 单个文件开始下载
        public Action<DownloadUpdateData> OnDownloadUpdate;  // 整体下载进度更新
        public Action<DownloadErrorData> OnDownloadError;    // 单个文件下载失败
        public Action<DownloaderFinishData> OnDownloadFinish;// 整个下载流程结束
    }
}