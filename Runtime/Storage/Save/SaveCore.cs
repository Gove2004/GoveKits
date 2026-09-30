using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 存档管理器，支持同步/异步 API 和原子写入，确保存档数据不会因意外中断而损坏。
    /// </summary>
    public static class SaveCore
    {
        private static string _rootPath;
        private static ISerializer _serializer;

        /// <summary>
        /// 初始化存档系统。设置根目录和序列化器。
        /// </summary>
        /// <param name="serializer">数据序列化器，为 null 时默认使用 JsonSerializer。</param>
        public static void Setup(ISerializer serializer)
        {
            _rootPath = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
            _serializer = serializer ?? new JsonSerializer();
        }

        /// <summary>
        /// 保存数据到指定路径（同步）。采用原子写入策略：先写入临时文件，再替换目标文件。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        /// <param name="data">要保存的数据对象。</param>
        public static void Save<T>(string relativePath, T data)
        {
            string fullPath = GetFullPath(relativePath);
            if (fullPath == null) return;

            byte[] bytes = _serializer.Serialize(data, typeof(T));

            string tempPath = fullPath + ".tmp";
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                // flushToDisk：确保数据落盘后再替换，防止断电导致存档损坏
                stream.Flush(true);
            }
            ReplaceAtomic(tempPath, fullPath);
        }

        /// <summary>
        /// 从指定路径加载数据（同步）。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        /// <returns>加载的数据对象，文件不存在时返回 default。</returns>
        public static T Load<T>(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath)) return default;

            byte[] bytes = File.ReadAllBytes(fullPath);
            return (T)_serializer.Deserialize(bytes, typeof(T));
        }

        /// <summary>
        /// 从指定路径加载数据，文件不存在时返回提供的默认值。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        /// <param name="defaultValue">文件不存在时的默认返回值。</param>
        /// <returns>加载的数据对象或默认值。</returns>
        public static T LoadOrDefault<T>(string relativePath, T defaultValue = default)
        {
            string fullPath = GetFullPath(relativePath);
            if (fullPath == null || !File.Exists(fullPath)) return defaultValue;

            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                return (T)_serializer.Deserialize(bytes, typeof(T));
            }
            catch (Exception e)
            {
                // 文件损坏/格式不符不应让读档链路崩溃，返回默认值并保留现场日志
                LogCore.Error(nameof(SaveCore), $"存档加载失败（已返回默认值）: {fullPath}\n{e}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 异步保存数据到指定路径。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        /// <param name="data">要保存的数据对象。</param>
        /// <param name="ct">取消令牌。</param>
        public static async UniTask SaveAsync<T>(string relativePath, T data, CancellationToken ct = default)
        {
            ISerializer serializer = _serializer;
            string fullPath = GetFullPath(relativePath);
            if (fullPath == null) return;

            byte[] bytes = serializer.Serialize(data, typeof(T));

            string tempPath = fullPath + ".tmp";
            await WriteAllBytesAsync(tempPath, bytes, ct);
            ReplaceAtomic(tempPath, fullPath);
        }

        /// <summary>
        /// 异步从指定路径加载数据。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>加载的数据对象，文件不存在时返回 default。</returns>
        public static async UniTask<T> LoadAsync<T>(string relativePath, CancellationToken ct = default)
        {
            ISerializer serializer = _serializer;
            string fullPath = GetFullPath(relativePath);
            if (!File.Exists(fullPath)) return default;

            byte[] bytes = await ReadAllBytesAsync(fullPath, ct);
            return (T)serializer.Deserialize(bytes, typeof(T));
        }

        /// <summary>
        /// 异步从指定路径加载数据，文件不存在时返回默认值。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        /// <param name="defaultValue">文件不存在时的默认返回值。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>加载的数据对象或默认值。</returns>
        public static async UniTask<T> LoadOrDefaultAsync<T>(string relativePath, T defaultValue = default, CancellationToken ct = default)
        {
            ISerializer serializer = _serializer;
            string fullPath = GetFullPath(relativePath);
            if (fullPath == null || !File.Exists(fullPath)) return defaultValue;

            try
            {
                byte[] bytes = await ReadAllBytesAsync(fullPath, ct);
                return (T)serializer.Deserialize(bytes, typeof(T));
            }
            catch (Exception e)
            {
                LogCore.Error(nameof(SaveCore), $"存档加载失败（已返回默认值）: {fullPath}\n{e}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 检查指定路径的存档是否存在。
        /// </summary>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        /// <returns>存档存在时返回 true。</returns>
        public static bool Exists(string relativePath)
            => File.Exists(GetFullPath(relativePath));

        /// <summary>
        /// 删除指定路径的存档文件。
        /// </summary>
        /// <param name="relativePath">相对于存档根目录的路径。</param>
        public static void Delete(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);
            if (fullPath == null) return;
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        /// <summary>
        /// 获取存档目录下所有匹配搜索模式的文件名（包含子目录）。
        /// </summary>
        /// <param name="searchPattern">搜索模式，默认为 "*"（全部）。</param>
        /// <returns>匹配的完整文件路径数组。</returns>
        public static string[] GetAllFiles(string searchPattern = "*")
        {
            if (string.IsNullOrEmpty(_rootPath)) return Array.Empty<string>();

            // 过滤原子写入残留的临时/备份文件
            return Directory.GetFiles(_rootPath, searchPattern, SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                            && !f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static string GetFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(_rootPath))
            {
                LogCore.Error(nameof(SaveCore), "请先调用 SaveCore.Setup 完成初始化，再执行存档操作");
                return null;
            }

            if (!Path.HasExtension(relativePath))
                relativePath = Path.ChangeExtension(relativePath, _serializer.FileExtension);

            string fullPath = Path.Combine(_rootPath, relativePath);

            // 路径穿越防护：归一化后必须仍位于存档根目录内，
            // 拦截 "../" 上跳与绝对路径注入，避免存档 API 被用于读写任意文件。
            // 补分隔符比对，防止 "Saves" 与 "Saves2" 这类同前缀目录误判通过。
            string normalized = Path.GetFullPath(fullPath);
            string rootWithSeparator = Path.GetFullPath(_rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!normalized.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                LogCore.Error(nameof(SaveCore), $"存档路径越界（不允许离开存档根目录），已拒绝操作: {relativePath}");
                return null;
            }

            string directory = Path.GetDirectoryName(normalized);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return normalized;
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
                string backupPath = targetPath + ".bak";
                try { if (File.Exists(targetPath)) File.Move(targetPath, backupPath); } catch { }

                try { File.Move(tempPath, targetPath); }
                catch
                {
                    try { if (File.Exists(backupPath)) File.Move(backupPath, targetPath); } catch { }
                    throw;
                }
                finally
                {
                    try { if (File.Exists(backupPath)) File.Delete(backupPath); } catch { }
                }
            }
        }

        private static async UniTask WriteAllBytesAsync(string path, byte[] bytes, CancellationToken ct)
        {
            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await stream.WriteAsync(bytes, 0, bytes.Length, ct);
            // flushToDisk：确保数据落盘后再替换，防止断电导致存档损坏
            stream.Flush(true);
        }

        private static async UniTask<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);

            if (stream.Length > int.MaxValue)
                throw new IOException($"文件过大: {stream.Length} 字节");

            byte[] buffer = new byte[stream.Length];
            int offset = 0;
            // 循环读取直到填满 buffer 或流结束，单次 ReadAsync 不保证读满
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, ct);
                if (read <= 0) break;
                offset += read;
            }

            if (offset < buffer.Length)
                Array.Resize(ref buffer, offset);

            return buffer;
        }

        /// <summary>
        /// 关闭存档系统。无外部资源需要清理。
        /// </summary>
        public static void Close()
        {
            // 无外部资源需要清理，文件由用户自行控制
        }
    }
}
