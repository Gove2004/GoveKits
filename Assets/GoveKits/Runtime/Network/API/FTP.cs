using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GoveKits.Network
{
    /// <summary>
    /// 专门处理大文件上传/下载 (FTP/HTTP)
    /// 避免内存峰值
    /// </summary>
    public static class NetFile
    {
        /// <summary>
        /// 下载文件 (流式直接写入硬盘，内存占用低)
        /// </summary>
        public static async UniTask<bool> DownloadFileAsync(
            string url, 
            string savePath, 
            IProgress<float> progress = null, 
            CancellationToken ct = default)
        {
            // 创建临时文件下载，成功后再重命名，防止文件损坏
            string tempPath = savePath + ".tmp";
            
            // 确保目录存在
            string dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                // 核心优化：使用 DownloadHandlerFile 避免加载到内存
                var handler = new DownloadHandlerFile(tempPath);
                handler.removeFileOnAbort = true;
                uwr.downloadHandler = handler;

                try
                {
                    await uwr.SendWebRequest().ToUniTask(progress: progress, cancellationToken: ct);

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        if (File.Exists(savePath)) File.Delete(savePath);
                        File.Move(tempPath, savePath);
                        return true;
                    }
                    else
                    {
                        LogManager.LogError("NetFile", $"Download Failed: {uwr.error}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.LogError("NetFile", $"Download Error: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        public static async UniTask<bool> UploadFileAsync(
            string url, 
            string filePath, 
            string fieldName = "file",
            IProgress<float> progress = null, 
            CancellationToken ct = default)
        {
            if (!File.Exists(filePath)) return false;

            // 如果是 FTP，UnityWebRequest.Put 更合适；如果是 HTTP Form，用 Post
            bool isFtp = url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);

            UnityWebRequest uwr;

            if (isFtp)
            {
                // FTP 上传
                uwr = UnityWebRequest.Put(url, File.ReadAllBytes(filePath)); // 注意：Put 不支持流式读取本地文件，大文件需慎重
            }
            else
            {
                // HTTP 表单上传
                WWWForm form = new WWWForm();
                // 这种方式会读入内存，如果是超大文件，建议使用 UploadHandlerFile (仅PUT支持较好) 
                // 或者分块上传。此处演示标准表单上传
                form.AddBinaryData(fieldName, File.ReadAllBytes(filePath), Path.GetFileName(filePath));
                uwr = UnityWebRequest.Post(url, form);
            }

            using (uwr)
            {
                try
                {
                    await uwr.SendWebRequest().ToUniTask(progress: progress, cancellationToken: ct);
                    return uwr.result == UnityWebRequest.Result.Success;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}