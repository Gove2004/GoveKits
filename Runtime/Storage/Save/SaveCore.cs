using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Storage.Save
{
    /// <summary>
    /// 存档序列化格式。
    /// </summary>
    public enum SerializerType
    {
        /// <summary>
        /// 使用 Json 序列化。
        /// </summary>
        Json = 0,

        /// <summary>
        /// 使用 Protobuf 序列化。
        /// </summary>
        Protobuf = 1,
    }

    /// <summary>
    /// 存档核心入口。
    /// <para>提供序列化器注册、格式切换、同步与异步存取能力。</para>
    /// </summary>
    public static class SaveCore
    {
        private const string SaveRootFolder = "Saves";
        private const string SaveFileExtension = ".save";
        private const string TempFileExtension = ".temp";
        public static SerializerType CurrentFormat { get; set; } = SerializerType.Json;
        private static readonly Dictionary<SerializerType, ISerializer> _serializers = new();
        private static string RootPath => Path.Combine(Application.persistentDataPath, SaveRootFolder);
        
        static SaveCore()
        {
            RegisterSerializer(SerializerType.Json, new JsonSerializer());
            RegisterSerializer(SerializerType.Protobuf, new ProtobufSerializer());

            if (!Directory.Exists(RootPath)) Directory.CreateDirectory(RootPath);
        }

        /// <summary>
        /// 注册序列化器实现。
        /// </summary>
        /// <param name="format">对应格式枚举。</param>
        /// <param name="serializer">序列化器实例。</param>
        public static void RegisterSerializer(SerializerType format, ISerializer serializer)
            => _serializers[format] = serializer;

        /// <summary>
        /// 获取指定格式的序列化器。
        /// </summary>
        /// <param name="format">目标格式。</param>
        /// <returns>序列化器实例。</returns>
        public static ISerializer GetSerializer(SerializerType format)
        {
            if (_serializers.TryGetValue(format, out var serializer))
            {
                return serializer;
            }

            throw new InvalidOperationException($"No serializer registered for format {format}");
        }

        /// <summary>
        /// 同步保存。
        /// </summary>
        /// <typeparam name="T">存档数据类型。</typeparam>
        /// <param name="saveable">存档对象。</param>
        public static void Save<T>(ISaveData<T> saveable)
        {
            var data = saveable.Save();
            var serializer = GetSerializer(CurrentFormat);
            byte[] bytes = serializer.Serialize(data, typeof(T));
            
            string fullPath = GetFullPath(saveable.RelativePath);
            string tempPath = fullPath + TempFileExtension;

            // 原子写入：先写入临时文件，再覆盖
            File.WriteAllBytes(tempPath, bytes);
            ReplaceFileAtomic(tempPath, fullPath);
        }

        /// <summary>
        /// 同步加载。
        /// </summary>
        /// <typeparam name="T">存档数据类型。</typeparam>
        /// <param name="saveable">存档对象。</param>
        public static void Load<T>(ISaveData<T> saveable)
        {
            string fullPath = GetFullPath(saveable.RelativePath);
            if (!File.Exists(fullPath)) return;

            byte[] bytes = File.ReadAllBytes(fullPath);
            T data = (T)GetSerializer(CurrentFormat).Deserialize(bytes, typeof(T));
            saveable.Load(data);
        }

        /// <summary>
        /// 异步保存。
        /// </summary>
        /// <typeparam name="T">存档数据类型。</typeparam>
        /// <param name="saveable">存档对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public static async UniTask SaveAsync<T>(ISaveData<T> saveable, CancellationToken cancellationToken = default)
        {
            var data = saveable.Save();
            var serializer = GetSerializer(CurrentFormat);
            byte[] bytes = serializer.Serialize(data, typeof(T));

            string fullPath = GetFullPath(saveable.RelativePath);
            string tempPath = fullPath + TempFileExtension;

            await WriteAllBytesAsync(tempPath, bytes, cancellationToken);
            ReplaceFileAtomic(tempPath, fullPath);
        }

        /// <summary>
        /// 异步加载。
        /// </summary>
        /// <typeparam name="T">存档数据类型。</typeparam>
        /// <param name="saveable">存档对象。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public static async UniTask LoadAsync<T>(ISaveData<T> saveable, CancellationToken cancellationToken = default)
        {
            string fullPath = GetFullPath(saveable.RelativePath);
            if (!File.Exists(fullPath)) return;

            byte[] bytes = await ReadAllBytesAsync(fullPath, cancellationToken);
            T data = (T)GetSerializer(CurrentFormat).Deserialize(bytes, typeof(T));
            saveable.Load(data);
        }

        /// <summary>
        /// 异步加载或默认。
        /// <para>当存档文件不存在时，使用提供的默认数据恢复存档对象状态。</para>
        /// </summary>
        /// <typeparam name="T">存档数据类型。</typeparam>
        /// <param name="saveable">存档对象。</param>
        /// <param name="defaultData">默认数据。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns></returns>
        public static async UniTask LoadOrDefaultAsync<T>(ISaveData<T> saveable, T defaultData, CancellationToken cancellationToken = default)
        {
            string fullPath = GetFullPath(saveable.RelativePath);
            if (!File.Exists(fullPath))
            {
                saveable.Load(defaultData);
                return;
            }

            byte[] bytes = await ReadAllBytesAsync(fullPath, cancellationToken);
            T data = (T)GetSerializer(CurrentFormat).Deserialize(bytes, typeof(T));
            saveable.Load(data);
        }

        /// <summary>
        /// 删除存档。
        /// <para>仅删除对应路径的存档文件，不修改 ISaveData 对象状态。</para>
        /// </summary>
        /// <typeparam name="T">存档数据类型。</typeparam>
        /// <param name="saveable">存档对象。</param>
        public static void Delete<T>(ISaveData<T> saveable)
        {
            string fullPath = GetFullPath(saveable.RelativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        /// <summary>
        /// 检查存档是否存在。
        /// </summary>
        /// <typeparam name="T">存档数据类型。</typeparam>
        /// <param name="saveable">存档对象。</param>
        public static bool Exists<T>(ISaveData<T> saveable)
        {
            string fullPath = GetFullPath(saveable.RelativePath);
            return File.Exists(fullPath);
        }

        #region Tools

        private static string GetFullPath(string relative)
        {
            string dir = Path.Combine(RootPath, Path.GetDirectoryName(relative) ?? "");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(RootPath, Path.ChangeExtension(relative, SaveFileExtension));
        }

        private static async UniTask WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private static async UniTask<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            if (stream.Length > int.MaxValue)
            {
                throw new IOException($"Save file too large: {stream.Length} bytes.");
            }

            byte[] bytes = new byte[(int)stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = await stream.ReadAsync(bytes, offset, bytes.Length - offset, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                offset += read;
            }

            if (offset == bytes.Length)
            {
                return bytes;
            }

            byte[] resized = new byte[offset];
            Buffer.BlockCopy(bytes, 0, resized, 0, offset);
            return resized;
        }

        private static void ReplaceFileAtomic(string tempPath, string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                File.Move(tempPath, fullPath);
                return;
            }

            try
            {
                File.Replace(tempPath, fullPath, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(fullPath);
                File.Move(tempPath, fullPath);
            }
        }

        #endregion
    }
}
