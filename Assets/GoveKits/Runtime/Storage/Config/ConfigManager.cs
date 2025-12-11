using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using GoveKits.Res; // 引用资源管理器

namespace GoveKits.Config
{
    /// <summary>
    /// 静态配置管理器
    /// <para>自动扫描所有 IConfigData 并通过 ResManager 加载对应 JSON</para>
    /// </summary>
    public static class ConfigManager
    {
        // 缓存字典：Type -> Dictionary<ID, ConfigObj>
        private static readonly Dictionary<Type, object> _configCache = new Dictionary<Type, object>();
        
        // 默认加载路径 (相对于 Resources 或 Addressables 的地址根目录)
        private static string _configRoot = "Config/Json"; 
        
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化配置表 (游戏启动时调用)
        /// </summary>
        /// <param name="rootPath">资源加载根路径，默认为 "Config/Json"</param>
        public static void Initialize(string rootPath = "Config/Json")
        {
            if (_isInitialized) return;

            _configRoot = rootPath;
            _configCache.Clear();
            
            LoadAllConfigs();
            
            _isInitialized = true;
            DebugLogger.LogGreen("ConfigManager", $"初始化完成，加载表数量: {_configCache.Count}");
        }

        private static void LoadAllConfigs()
        {
            // 策略改变：
            // 在移动端无法遍历文件夹，因此反向操作：
            // 1. 反射查找所有实现了 IConfigData 的 Config 类
            // 2. 根据类名推断 JSON 文件名 (约定：类名去掉 "Config" 后缀即为文件名)
            // 3. 调用 ResManager 加载
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();
            
            // 获取内部加载方法的反射信息
            MethodInfo loadMethod = typeof(ConfigManager).GetMethod("LoadConfigInternal", BindingFlags.Static | BindingFlags.NonPublic);

            foreach (Type type in types)
            {
                // 筛选条件：是类、非抽象、实现了 IConfigData 接口
                if (type.IsClass && !type.IsAbstract && typeof(IConfigData).IsAssignableFrom(type))
                {
                    // 推断文件名
                    // 例如类名 "Item_WeaponConfig" -> 文件名 "Item_Weapon"
                    string typeName = type.Name;
                    if (typeName.EndsWith("Config"))
                    {
                        string fileName = typeName.Substring(0, typeName.Length - 6);
                        string fullPath = $"{_configRoot}/{fileName}";

                        // 泛型调用 LoadConfigInternal<T>(path)
                        MethodInfo genericMethod = loadMethod.MakeGenericMethod(type);
                        genericMethod.Invoke(null, new object[] { fullPath });
                    }
                }
            }
        }

        // 内部泛型加载
        private static void LoadConfigInternal<T>(string path) where T : class
        {
            // 使用 ResManager 加载 TextAsset (Unity 中 Json 被识别为 TextAsset)
            TextAsset jsonAsset = ResManager.Load<TextAsset>(path);
            
            if (jsonAsset == null)
            {
                // 某些 Config 类可能没有对应的 json 文件 (比如基类或未生成的表)，跳过
                DebugLogger.LogWarning("ConfigManager", $"未找到文件: {path}"); 
                return;
            }

            string json = jsonAsset.text;
            
            // 如果使用引用计数模式，加载完 TextAsset 内容后可以立即释放资源句柄
            // 因为我们已经把内容反序列化到 _configCache 内存里了
            ResManager.Release(path);

            try
            {
                object data = null;
                
                // 1. 尝试反序列化为 int Key 字典
                try 
                {
                    data = JsonConvert.DeserializeObject<Dictionary<int, T>>(json);
                } 
                catch {}

                // 2. 如果失败，尝试反序列化为 string Key 字典
                if (data == null)
                {
                    try 
                    {
                        data = JsonConvert.DeserializeObject<Dictionary<string, T>>(json);
                    } 
                    catch {}
                }

                if (data != null)
                {
                    _configCache[typeof(T)] = data;
                }
                else
                {
                    DebugLogger.LogError("ConfigManager", $"反序列化失败或数据为空: {path}");
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogError("ConfigManager", $"解析异常 {path}: {e.Message}");
            }
        }

        #region 公开 API

        /// <summary>
        /// 获取单条配置 (Int ID)
        /// </summary>
        public static T Get<T>(int id) where T : class
        {
            if (!_isInitialized) 
            {
                DebugLogger.LogError("ConfigManager", "未初始化！请先调用 Initialize()");
                return null;
            }

            Type type = typeof(T);
            if (_configCache.TryGetValue(type, out object dictObj))
            {
                if (dictObj is Dictionary<int, T> dictInt && dictInt.TryGetValue(id, out T res)) return res;
            }
            
            DebugLogger.LogError("Config", $"未找到配置 {type.Name} ID:{id}");
            return null;
        }

        /// <summary>
        /// 获取单条配置 (String ID)
        /// </summary>
        public static T Get<T>(string id) where T : class
        {
            if (!_isInitialized) 
            {
                DebugLogger.LogError("ConfigManager", "未初始化！请先调用 Initialize()");
                return null;
            }

            Type type = typeof(T);
            if (_configCache.TryGetValue(type, out object dictObj))
            {
                if (dictObj is Dictionary<string, T> dictStr && dictStr.TryGetValue(id, out T res)) return res;
            }

            DebugLogger.LogError("Config", $"未找到配置 {type.Name} ID:{id}");
            return null;
        }

        /// <summary>
        /// 获取整张表 (Int Key)
        /// </summary>
        public static Dictionary<int, T> GetDictInt<T>() where T : class
        {
            if (_configCache.TryGetValue(typeof(T), out object dictObj))
                return dictObj as Dictionary<int, T>;
            return null;
        }
        
        /// <summary>
        /// 获取整张表 (String Key)
        /// </summary>
        public static Dictionary<string, T> GetDictStr<T>() where T : class
        {
            if (_configCache.TryGetValue(typeof(T), out object dictObj))
                return dictObj as Dictionary<string, T>;
            return null;
        }

        /// <summary>
        /// 清理所有配置缓存
        /// </summary>
        public static void Clear()
        {
            _configCache.Clear();
            _isInitialized = false;
        }

        #endregion
    }
}