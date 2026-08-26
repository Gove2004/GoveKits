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
                    LogCore.Error(nameof(ConfigCore), $"加载 {binding.ConfigType.Name} 失败: {e.Message}");
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

                var method = typeof(IConfigParser).GetMethod(nameof(IConfigParser.Parse), BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                    throw new InvalidOperationException("IConfigParser.Parse 方法不存在");
                var genericMethod = method.MakeGenericMethod(binding.ConfigType);
                var result = genericMethod.Invoke(parser, new object[] { textAsset.bytes, textAsset.text });

                return (List<IConfigData>)result;
            }
            finally
            {
                handle?.Release();
            }
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
        /// 快捷别名：等价于 Load&lt;T&gt;(predicate)。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <param name="predicate">过滤条件。</param>
        /// <returns>匹配过滤条件的配置对象列表。</returns>
        public static List<T> Get<T>(Func<T, bool> predicate) where T : class, IConfigData
            => LoadInternal<T>(predicate);

        /// <summary>
        /// 加载指定类型的全部配置数据，不进行过滤。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <returns>该类型的所有配置对象列表。</returns>
        public static List<T> LoadAll<T>() where T : class, IConfigData
            => LoadInternal<T>(null);

        /// <summary>
        /// 快捷别名：等价于 LoadAll&lt;T&gt;。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <returns>该类型的所有配置对象列表。</returns>
        public static List<T> GetAll<T>() where T : class, IConfigData
            => LoadInternal<T>(null);

        /// <summary>
        /// 加载指定类型的首条匹配配置。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <param name="predicate">过滤条件。</param>
        /// <returns>第一条匹配的配置对象，若无匹配则返回 null。</returns>
        public static T LoadOne<T>(Func<T, bool> predicate) where T : class, IConfigData
            => Get(predicate).FirstOrDefault();

        /// <summary>
        /// 快捷别名：等价于 LoadOne&lt;T&gt;(predicate)。
        /// </summary>
        /// <typeparam name="T">配置数据类型。</typeparam>
        /// <param name="predicate">过滤条件。</param>
        /// <returns>第一条匹配的配置对象，若无匹配则返回 null。</returns>
        public static T GetOne<T>(Func<T, bool> predicate) where T : class, IConfigData
            => LoadOne(predicate);

        private static List<T> LoadInternal<T>(Func<T, bool> predicate) where T : class, IConfigData
        {
            if (!_configTables.TryGetValue(typeof(T), out var table))
            {
                LogCore.Warning(nameof(ConfigCore), $"配置表未加载: {typeof(T).Name}");
                return new List<T>();
            }

            var result = table.OfType<T>().Where(predicate ?? (_ => true)).ToList();
            return result;
        }

        public static void Close()
        {
            _configTables.Clear();
        }
    }
}
