using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GoveKits.Runtime.Core;
using UnityEngine;


namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 配置核心 - 基于 ResCore 加载资源
    /// </summary>
    public static class ConfigCore
    {
        private static List<IConfigParser> _parsers = new();
        private static Dictionary<Type, List<IConfigData>> _configTables = new();
        private static MethodInfo _parseMethod;

        public static void InfuseParser(IConfigParser parser) => _parsers.Add(parser);

        /// <summary>
        /// 注入足够 IParser 再初始化
        /// </summary>
        public static void Initialize()
        {
            _parseMethod = typeof(IConfigParser).GetMethod(nameof(IConfigParser.Parse));

            var bindings = ConfigBindingScanner.Scan();
            _configTables.Clear();

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
            var textAsset = handle.AssetObject as TextAsset;
            
            // 根据文件扩展名自动选择解析器
            var parser = _parsers.FirstOrDefault(p => p.Extensions.Contains(binding.Attribute.Extension, StringComparer.OrdinalIgnoreCase));
            
            if (parser == null)
            {
                throw new NotSupportedException($"不支持的配置文件格式: {binding.Attribute.Extension}，路径: {binding.Attribute.FilePath}");
            }

            // 反射调用泛型解析
            var method = _parseMethod.MakeGenericMethod(binding.ConfigType);
            var result = method.Invoke(parser, new object[] { textAsset.bytes, textAsset.text });
            
            // 转换结果
            var list = new List<IConfigData>();
            if (result is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is IConfigData data) list.Add(data);
                }
            }

            handle.Release();
            return list;
        }

        public static List<T> Load<T>(Func<T, bool> predicate) where T : class, IConfigData
        {
            if (!_configTables.TryGetValue(typeof(T), out var table))
            {
                LogCore.Warning(nameof(ConfigCore), $"配置表未加载: {typeof(T).Name}");
                return new List<T>();
            }

            return table.Cast<T>().Where(predicate).ToList();
        }

        public static List<T> LoadAll<T>() where T : class, IConfigData
        {
            if (!_configTables.TryGetValue(typeof(T), out var table))
            {
                LogCore.Warning(nameof(ConfigCore), $"配置表未加载: {typeof(T).Name}");
                return new List<T>();
            }

            return table.Cast<T>().ToList();
        }

        public static T LoadOne<T>(Func<T, bool> predicate) where T : class, IConfigData
        {
            return Load(predicate).FirstOrDefault();
        }

        public static void Clear()
        {
            _configTables.Clear();
            _parsers.Clear();
        }
    }
}