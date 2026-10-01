using System;
using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 热更新回调集合，覆盖版本检查、清单更新和资源下载三个阶段。
    /// 回调参数类型对应 YooAsset 3.x 下载器事件参数（readonly struct）。
    /// </summary>
    public class UpdateCallbacks
    {
        // 阶段 1: 检查版本

        /// <summary>开始检查版本时触发。</summary>
        public Action OnCheckVersionBegin;

        /// <summary>版本检查成功时触发，参数为最新版本号。</summary>
        public Action<string> OnCheckVersionSuccess;

        /// <summary>版本检查失败时触发，参数为错误信息。</summary>
        public Action<string> OnCheckVersionFailed;

        // 阶段 2: 更新清单

        /// <summary>开始更新清单时触发。</summary>
        public Action OnUpdateManifestBegin;

        /// <summary>清单更新成功时触发。</summary>
        public Action OnUpdateManifestSuccess;

        /// <summary>清单更新失败时触发，参数为错误信息。</summary>
        public Action<string> OnUpdateManifestFailed;

        // 阶段 3: 资源下载

        /// <summary>开始下载时触发，参数为总文件数和总字节数。</summary>
        public Action<int, long> OnDownloadBegin;

        /// <summary>每个文件开始下载时触发。</summary>
        public Action<DownloadFileStartedEventArgs> OnDownloadFileBegin;

        /// <summary>下载进度更新时触发。</summary>
        public Action<DownloadProgressChangedEventArgs> OnDownloadUpdate;

        /// <summary>下载发生错误时触发。</summary>
        public Action<DownloadErrorEventArgs> OnDownloadError;

        /// <summary>所有文件下载完成时触发（无论成功失败）。</summary>
        public Action<DownloadCompletedEventArgs> OnDownloadFinish;
    }
}
