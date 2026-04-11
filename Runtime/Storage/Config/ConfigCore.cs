using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 配置核心 - 基于 ResCore 加载资源，支持 Resources/AB/Addressables 等任意来源
    /// </summary>
    public class ConfigCore : ICore
    {
        private readonly List<IConfigParser> _parsers = new();
        private readonly Dictionary<Type, List<IConfigData>> _configTables = new();
        private readonly MethodInfo _parseMethod;

        public ConfigCore(IConfigParser[] parsers)
        {
            _parsers.AddRange(parsers ?? Array.Empty<IConfigParser>());
            _parseMethod = typeof(IConfigParser).GetMethod(nameof(IConfigParser.Parse));

            Init();
        }

        /// <summary>
        /// 扫描并加载全部配置表
        /// </summary>
        public void Init()
        {
            var bindings = ConfigBindingScanner.Scan();
            _configTables.Clear();

            foreach (var binding in bindings)
            {
                try
                {
                    var rows = LoadTable(binding);
                    _configTables[binding.ConfigType] = rows;
                    CoreLocator.Log.Info(nameof(ConfigCore), $"已加载 {binding.ConfigType.Name} ({rows.Count} 行)");
                }
                catch (Exception e)
                {
                    CoreLocator.Log.Error(nameof(ConfigCore), $"加载 {binding.ConfigType.Name} 失败: {e.Message}");
                }
            }

            CoreLocator.Log.Success(nameof(ConfigCore), $"配置系统初始化完成，共 {bindings.Count} 个配置表");
        }

        private List<IConfigData> LoadTable(ConfigBinding binding)
        {
            // 关键：通过 ResCore 加载 TextAsset，自动处理 Resources/AB/Addressables 路径
            var handle = CoreLocator.Res.Load<TextAsset>(binding.Attribute.FilePath);
            
            if (!handle.IsValid || handle.Asset == null)
            {
                throw new FileNotFoundException($"配置资源不存在: {binding.Attribute.FilePath}");
            }

            var textAsset = handle.Asset;
            
            // 根据文件扩展名自动选择解析器
            string ext = Path.GetExtension(binding.Attribute.FilePath).ToLowerInvariant();
            var parser = _parsers.FirstOrDefault(p => p.Extensions.Contains(ext));
            
            if (parser == null)
            {
                throw new NotSupportedException($"不支持的配置文件格式: {ext}，路径: {binding.Attribute.FilePath}");
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

            return list;
        }

        public List<T> Load<T>(Func<T, bool> predicate) where T : class, IConfigData
        {
            if (!_configTables.TryGetValue(typeof(T), out var table))
            {
                CoreLocator.Log.Warn(nameof(ConfigCore), $"配置表未加载: {typeof(T).Name}");
                return new List<T>();
            }

            return table.Cast<T>().Where(predicate).ToList();
        }

        public List<T> LoadAll<T>() where T : class, IConfigData
        {
            if (!_configTables.TryGetValue(typeof(T), out var table))
            {
                CoreLocator.Log.Warn(nameof(ConfigCore), $"配置表未加载: {typeof(T).Name}");
                return new List<T>();
            }

            return table.Cast<T>().ToList();
        }

        public T LoadOne<T>(Func<T, bool> predicate) where T : class, IConfigData
        {
            return Load(predicate).FirstOrDefault();
        }

        public void OnShutdown()
        {
            _configTables.Clear();
            _parsers.Clear();
        }
    }
}