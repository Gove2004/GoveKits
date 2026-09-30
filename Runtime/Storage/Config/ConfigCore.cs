using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 配置表核心。基于 ResCore 加载资源文件，通过 IConfigParser 解析配置数据并提供查询 API。
    /// </summary>
    public static class ConfigCore
    {
        private static readonly List<IConfigParser> _parsers = new();
        private static readonly Dictionary<Type, List<IConfigData>> _configTables = new();
        private static readonly Dictionary<Type, MethodInfo> _parseMethodCache = new();
        private static readonly Dictionary<Type, IEnumerable<IConfigData>> _resolvedTables = new();

        /// <summary>
        /// 添加一个配置解析器。解析器按注册的顺序参与格式匹配。
        /// </summary>
        /// <param name="parser">要注册的解析器实例，不可为 null。</param>
        public static void AddParser(IConfigParser parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (_parsers.Contains(parser)) return;
            _parsers.Add(parser);
        }

        /// <summary>
        /// 初始化配置系统。扫描所有标注了 [ConfigPath] 的类型并自动加载对应的配置表。
        /// </summary>
        public static void Setup()
        {
            var bindings = ConfigBindingScanner.Scan();
            // 清除上次 Setup 遗留的表：重复 Setup（如热重载）时旧数据不能残留，
            // 否则本次加载失败的表会继续返回旧数据（静默脏读）
            _configTables.Clear();
            _resolvedTables.Clear();

            foreach (var binding in bindings)
            {
                try
                {
                    var rows = LoadTable(binding);
                    _configTables[binding.ConfigType] = rows;
                    LogCore.Info(nameof(ConfigCore), $"已加载 {binding.ConfigType.Name} ({rows.Count} 行)");
                }
                catch (Exception e)
                {
                    // 加载失败时不写入占位空表，避免查询端误判为"表存在但无数据"
                    LogCore.Error(nameof(ConfigCore), $"加载配置表 {binding.ConfigType.Name} 失败（路径: {binding.Attribute.FilePath}）: {e.Message}{(e.InnerException != null ? $" | 原因: {e.GetBaseException().Message}" : string.Empty)}");
                }
            }

            LogCore.Success(nameof(ConfigCore), $"配置系统初始化完成，共 {bindings.Count} 个配置表");
        }

        private static List<IConfigData> LoadTable(ConfigBinding binding)
        {
            var handle = ResCore.LoadAssetSync<TextAsset>(binding.Attribute.FilePath);
            var textAsset = handle?.AssetObject as TextAsset;
            if (textAsset == null)
                throw new FileNotFoundException($"配置资源加载失败: {binding.Attribute.FilePath} (类型: {binding.ConfigType.Name})");

            try
            {
                var parser = _parsers.FirstOrDefault(p => p.Extensions.Contains(binding.Attribute.Extension, StringComparer.OrdinalIgnoreCase));
                if (parser == null)
                    throw new NotSupportedException($"不支持的配置文件格式: {binding.Attribute.Extension}，路径: {binding.Attribute.FilePath}");

                var parseMethod = GetParseMethod(binding.ConfigType);
                var result = parseMethod.Invoke(parser, new object[] { textAsset.bytes, textAsset.text });

                return (List<IConfigData>)result;
            }
            finally
            {
                handle?.Release();
            }
        }

        /// <summary>获取 IConfigParser.Parse 的指定泛型实例并缓存，避免每次加载都做反射查找。</summary>
        private static MethodInfo GetParseMethod(Type configType)
        {
            if (!_parseMethodCache.TryGetValue(configType, out var method))
            {
                method = typeof(IConfigParser).GetMethod(nameof(IConfigParser.Parse), BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                    throw new InvalidOperationException("IConfigParser.Parse 方法不存在");
                method = method.MakeGenericMethod(configType);
                _parseMethodCache[configType] = method;
            }
            return method;
        }

        /// <summary>
        /// 加载指定类型的配置表，按谓词过滤。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <param name="predicate">过滤条件；返回 true 的数据会被保留。</param>
        /// <returns>匹配过滤条件的配置对象列表。</returns>
        public static List<T> Load<T>(Func<T, bool> predicate) where T : class, IConfigData
            => LoadInternal<T>(predicate);

        /// <summary>
        /// 加载指定类型的全部配置数据，不进行过滤。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <returns>该类型的所有配置对象列表。</returns>
        public static List<T> LoadAll<T>() where T : class, IConfigData
            => LoadInternal<T>(null);

        /// <summary>
        /// 加载指定类型的首条匹配配置。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <param name="predicate">过滤条件。</param>
        /// <returns>第一条匹配的配置对象，若无匹配则返回 null。</returns>
        public static T LoadOne<T>(Func<T, bool> predicate) where T : class, IConfigData
            => Load(predicate).FirstOrDefault();

        private static List<T> LoadInternal<T>(Func<T, bool> predicate) where T : class, IConfigData
        {
            // 精确 key 未命中时，回退遍历所有已加载表按类型过滤（接口类型注册时以具体子类作 key）
            if (!_configTables.TryGetValue(typeof(T), out var table))
            {
                table = ResolveTable<T>()?.ToList();
                if (table == null)
                {
                    LogCore.Warning(nameof(ConfigCore), $"配置表未加载: {typeof(T).Name}");
                    return new List<T>();
                }
            }

            var result = table.OfType<T>().Where(predicate ?? (_ => true)).ToList();
            return result;
        }

        /// <summary>从所有已加载表中查找元素类型兼容 T 的表，合并全部命中表的数据并缓存。</summary>
        private static IEnumerable<IConfigData> ResolveTable<T>() where T : class, IConfigData
        {
            if (_resolvedTables.TryGetValue(typeof(T), out var cached))
                return cached;

            List<IConfigData> merged = null;
            int hitCount = 0;
            foreach (var kvp in _configTables)
            {
                // 以注册键（具体配置类型）判断兼容性：空表没有元素可供 is 推断，
                // 按 key 判定可让空表也参与命中（表存在但无数据 ≠ 表未加载）
                if (!typeof(T).IsAssignableFrom(kvp.Key)) continue;

                hitCount++;
                merged ??= new List<IConfigData>();
                merged.AddRange(kvp.Value);
            }

            if (merged == null) return null;

            // 接口类型（如本地化接口）常见"每子类一张表"：单表命中会静默丢失其余表数据
            if (hitCount > 1)
                LogCore.Warning(nameof(ConfigCore), $"接口 {typeof(T).Name} 回退命中 {hitCount} 张配置表，已合并全部 {merged.Count} 条数据。");

            _resolvedTables[typeof(T)] = merged;
            return merged;
        }

        public static void Close()
        {
            _configTables.Clear();
            _resolvedTables.Clear();
        }
    }
}
