using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace GoveKits.Runtime.Storage.Config
{
    /// <summary>
    /// 配置来源类型。
    /// </summary>
    public enum ConfigSourceType
    {
        Resources = 0,
        StreamingAssets = 1,
    }

    /// <summary>
    /// 配置文件格式。
    /// </summary>
    public enum ConfigFileType
    {
        Json = 0,
        Csv = 1,
    }

    /// <summary>
    /// 配置读取核心。
    /// <para>扫描所有带 ConfigAttribute 的类型并加载到内存。</para>
    /// <para>提供 Load 系列查询接口。</para>
    /// </summary>
    public static class ConfigCore
    {
        private static readonly Dictionary<ConfigFileType, IConfigParser> Parsers = new();
        private static readonly Dictionary<Type, List<IConfigData>> ConfigTables = new();
        private static readonly MethodInfo ParseMethodDefinition =
            typeof(IConfigParser).GetMethod(nameof(IConfigParser.Parse))
            ?? throw new InvalidOperationException("IConfigParser.Parse method not found.");

        private static bool IsInitialized;
        public static bool Initialized => IsInitialized;

        static ConfigCore()
        {
            Parsers[ConfigFileType.Json] = new JsonConfigParser();
            Parsers[ConfigFileType.Csv] = new CsvConfigParser();
        }

        /// <summary>
        /// 扫描并加载全部配置表。
        /// </summary>
        /// <remarks>
        /// 该方法会清空已有缓存并重建内存表。
        /// 建议在游戏启动流程中只调用一次。
        /// </remarks>
        public static async UniTask InitAsync(CancellationToken cancellationToken = default)
        {
            IsInitialized = false;
            List<ConfigBinding> bindings = ConfigBindingScanner.Scan();
            int loadedTableCount = 0;
            var loadedTableNames = new StringBuilder(256);

            ConfigTables.Clear();

            for (int i = 0; i < bindings.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Type configType = bindings[i].ConfigType;
                ConfigAttribute binding = bindings[i].Attribute;
                List<IConfigData> rows = await LoadTableAsync(configType, binding, cancellationToken);
                ConfigTables[configType] = rows;

                loadedTableCount++;
                if (loadedTableNames.Length > 0)
                {
                    loadedTableNames.Append(" | ");
                }

                loadedTableNames
                    .Append(configType.Name)
                    .Append(" -> ")
                    .Append(binding.FilePath)
                    .Append(" (")
                    .Append(rows.Count)
                    .Append(')');
            }

            IsInitialized = true;
            LogCore.LogColor(nameof(ConfigCore), $"Init complete. Loaded {loadedTableCount} table(s). {loadedTableNames}", "00FF00");
        }

        /// <summary>
        /// 使用 Lambda 条件筛选配置。
        /// </summary>
        public static List<T> Load<T>(Func<T, bool> predicate)
            where T : class, IConfigData
        {
            EnsureInitialized();

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            List<T> result = new();
            List<IConfigData> table = GetTable(typeof(T));
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i] is T item && predicate(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取单个配置表的全部数据。
        /// </summary>
        public static List<T> LoadAll<T>()
            where T : class, IConfigData
        {
            EnsureInitialized();

            List<IConfigData> table = GetTable(typeof(T));
            List<T> result = new(table.Count);
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i] is T item)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static string BuildStreamingAssetLocation(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("relativePath can not be empty.", nameof(relativePath));
            }

            string normalized = relativePath.Replace('\\', '/').TrimStart('/');
            return Path.Combine(Application.streamingAssetsPath, normalized);
        }

        private static bool NeedWebRequest(string location)
            => location.Contains("://", StringComparison.OrdinalIgnoreCase) || location.StartsWith("jar:", StringComparison.OrdinalIgnoreCase);

        private static async UniTask<List<IConfigData>> LoadTableAsync(Type configType, ConfigAttribute binding, CancellationToken cancellationToken)
        {
            if (!TryGetParser(binding.ParseType, out IConfigParser parser))
            {
                throw new InvalidOperationException($"No parser registered for parseType={binding.ParseType}.");
            }

            (byte[] bytes, string text) = await ReadRawContentAsync(binding, cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                return new List<IConfigData>();
            }

            // IConfigParser.Parse<T> 是泛型接口，这里在运行时按目标配置类型闭包调用。
            MethodInfo parseMethod = ParseMethodDefinition.MakeGenericMethod(configType);
            object parsed = parseMethod.Invoke(parser, new object[] { bytes, text });
            var rows = new List<IConfigData>();
            if (parsed is System.Collections.IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item is IConfigData config)
                    {
                        rows.Add(config);
                    }
                }
            }

            return rows;
        }

        private static async UniTask<(byte[] bytes, string text)> ReadRawContentAsync(ConfigAttribute binding, CancellationToken cancellationToken)
        {
            if (binding.SourceType == ConfigSourceType.Resources)
            {
                await UniTask.Yield(cancellationToken);
                TextAsset asset = Resources.Load<TextAsset>(NormalizeResourcePath(binding.FilePath));
                return asset == null ? (null, null) : (asset.bytes, asset.text);
            }

            byte[] bytes = await LoadStreamingBytesAsync(binding.FilePath, cancellationToken);
            string text = IsTextParse(binding.ParseType) ? Encoding.UTF8.GetString(bytes) : null;
            return (bytes, text);
        }

        private static async UniTask<byte[]> LoadStreamingBytesAsync(string relativePath, CancellationToken cancellationToken)
        {
            string location = BuildStreamingAssetLocation(relativePath);
            if (NeedWebRequest(location))
            {
                // Android/WebGL 等平台可能是 jar/file/http URI，必须走 UnityWebRequest。
                return await LoadBytesByWebRequest(location, cancellationToken);
            }

            return await UniTask.RunOnThreadPool(() => File.ReadAllBytes(location), cancellationToken: cancellationToken);
        }

        private static List<IConfigData> GetTable(Type type)
        {
            if (!ConfigTables.TryGetValue(type, out List<IConfigData> table))
            {
                throw new InvalidOperationException($"Type {type.FullName} was not loaded. Ensure ConfigAttribute exists and InitAsync was called.");
            }

            return table;
        }

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("ConfigCore is not loaded. Call InitAsync first.");
            }
        }

        private static bool IsTextParse(ConfigFileType parseType)
        {
            return parseType == ConfigFileType.Json || parseType == ConfigFileType.Csv;
        }

        private static string NormalizeResourcePath(string path)
        {
            string normalized = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
            string ext = Path.GetExtension(normalized);
            if (!string.IsNullOrEmpty(ext))
            {
                normalized = normalized.Substring(0, normalized.Length - ext.Length);
            }

            return normalized;
        }

        private static bool TryGetParser(ConfigFileType parseType, out IConfigParser parser)
        {
            return Parsers.TryGetValue(parseType, out parser);
        }

        private static async UniTask<byte[]> LoadBytesByWebRequest(string location, CancellationToken cancellationToken)
        {
            using UnityWebRequest request = UnityWebRequest.Get(location);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"Load config failed: {location}, error={request.error}");
            }

            return request.downloadHandler.data;
        }
    }
}
