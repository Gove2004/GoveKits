using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 存档管理器：无侵入式，直接存取 POCO 对象。
    /// </summary>
    public static class SaveCore
    {
        private static string _rootPath;
        private static ISerializer _serializer;

        public static void Initialize(ISerializer serializer, string rootFolder = "Saves")
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _rootPath = Path.Combine(Application.persistentDataPath, rootFolder);
            
            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        #region 同步 API

        /// <summary>
        /// 保存数据到指定路径。
        /// </summary>
        /// <param name="relativePath">相对路径（如 "player.data" 或 "slot1/player.json"）</param>
        /// <param name="data">要保存的数据对象</param>
        public static void Save<T>(string relativePath, T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            string fullPath = GetFullPath(relativePath);
            byte[] bytes = _serializer.Serialize(data, typeof(T));
            
            // 原子写入
            string tempPath = fullPath + ".tmp";
            File.WriteAllBytes(tempPath, bytes);
            ReplaceAtomic(tempPath, fullPath);
        }

        /// <summary>
        /// 从指定路径加载数据。
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>反序列化后的对象，文件不存在则返回 null</returns>
        public static T Load<T>(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath)) return default;

            byte[] bytes = File.ReadAllBytes(fullPath);
            return (T)_serializer.Deserialize(bytes, typeof(T));
        }

        /// <summary>
        /// 加载数据，不存在则返回默认值。
        /// </summary>
        public static T LoadOrDefault<T>(string relativePath, T defaultValue = default)
        {
            string fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath)) return defaultValue;

            byte[] bytes = File.ReadAllBytes(fullPath);
            return (T)_serializer.Deserialize(bytes, typeof(T));
        }

        #endregion

        #region 异步 API

        public static async UniTask SaveAsync<T>(string relativePath, T data, CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            
            string fullPath = GetFullPath(relativePath);
            byte[] bytes = _serializer.Serialize(data, typeof(T));
            
            string tempPath = fullPath + ".tmp";
            await WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            ReplaceAtomic(tempPath, fullPath);
        }

        public static async UniTask<T> LoadAsync<T>(string relativePath, CancellationToken cancellationToken = default)
        {
            string fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath)) return default;

            byte[] bytes = await ReadAllBytesAsync(fullPath, cancellationToken);
            return (T)_serializer.Deserialize(bytes, typeof(T));
        }

        public static async UniTask<T> LoadOrDefaultAsync<T>(string relativePath, T defaultValue = default, CancellationToken cancellationToken = default)
        {
            string fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath)) return defaultValue;

            byte[] bytes = await ReadAllBytesAsync(fullPath, cancellationToken);
            return (T)_serializer.Deserialize(bytes, typeof(T));
        }

        #endregion

        #region 文件操作

        /// <summary>
        /// 检查存档是否存在。
        /// </summary>
        public static bool Exists(string relativePath)
        {
            return File.Exists(GetFullPath(relativePath));
        }

        /// <summary>
        /// 删除存档。
        /// </summary>
        public static void Delete(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        /// <summary>
        /// 获取所有存档文件名（含子目录）。
        /// </summary>
        public static string[] GetAllFiles(string searchPattern = "*")
        {
            return Directory.GetFiles(_rootPath, searchPattern, SearchOption.AllDirectories);
        }

        #endregion

        #region 工具方法

        private static string GetFullPath(string relativePath)
        {
            // 自动处理扩展名（如果用户没加，就加上序列器推荐的）
            if (!Path.HasExtension(relativePath))
                relativePath = Path.ChangeExtension(relativePath, _serializer.FileExtension);
                
            // 安全路径拼接
            string fullPath = Path.Combine(_rootPath, relativePath);
            string directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
                
            return fullPath;
        }

        private static void ReplaceAtomic(string tempPath, string targetPath)
        {
            try
            {
                if (File.Exists(targetPath))
                    File.Replace(tempPath, targetPath, null);
                else
                    File.Move(tempPath, targetPath);
            }
            catch (PlatformNotSupportedException)
            {
                // 某些平台不支持 Replace，退化为删除+移动
                if (File.Exists(targetPath))
                    File.Delete(targetPath);
                File.Move(tempPath, targetPath);
            }
        }

        private static async UniTask WriteAllBytesAsync(string path, byte[] bytes, CancellationToken ct)
        {
            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await stream.WriteAsync(bytes, 0, bytes.Length, ct);
        }

        private static async UniTask<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            
            if (stream.Length > int.MaxValue)
                throw new IOException($"File too large: {stream.Length} bytes");

            byte[] buffer = new byte[stream.Length];
            int read = await stream.ReadAsync(buffer, 0, (int)stream.Length, ct);
            
            if (read < buffer.Length)
                Array.Resize(ref buffer, read);
                
            return buffer;
        }

        #endregion
    }
}