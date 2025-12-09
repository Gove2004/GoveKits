using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GoveKits.Singleton;
using Newtonsoft.Json;
using UnityEngine;



namespace Game.Config
{
    public interface IConfigData { } 
}




namespace GoveKits.Config
{
    public class ConfigManager : MonoSingleton<ConfigManager>
    {
        private readonly Dictionary<Type, object> _configCache = new Dictionary<Type, object>();
        private string _configPath = "Assets/Config/Json";
        private string _namespaceName = "Game.Config"; 

        public void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            _configCache.Clear();
            LoadAllConfigsAutomatically();
        }

        private void LoadAllConfigsAutomatically()
        {
            if (!Directory.Exists(_configPath)) return;

            string[] files = Directory.GetFiles(_configPath, "*.json");
            MethodInfo loadMethod = GetType().GetMethod("LoadConfigInternal", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (string filePath in files)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    // 推导类名 (需与Editor生成规则一致)
                    string className = $"{_namespaceName}.{fileName}Config";
                    Type configType = Assembly.GetExecutingAssembly().GetType(className);

                    if (configType == null) continue;

                    // 动态调用泛型加载方法
                    MethodInfo genericMethod = loadMethod.MakeGenericMethod(configType);
                    genericMethod.Invoke(this, new object[] { fileName });
                }
                catch (Exception e)
                {
                    Debug.LogError($"自动加载失败: {filePath} - {e.Message}");
                }
            }
            Debug.Log($"[ConfigManager] 初始化完成，缓存表数量: {_configCache.Count}");
        }

        private void LoadConfigInternal<T>(string fileName) where T : class
        {
            Type type = typeof(T);
            string path = Path.Combine(_configPath, fileName + ".json");
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            try
            {
                object data = null;
                try
                {
                    data = JsonConvert.DeserializeObject<Dictionary<int, T>>(json);
                }
                catch
                {
                    data = JsonConvert.DeserializeObject<Dictionary<string, T>>(json);
                }

                if (data != null) _configCache[type] = data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Json解析错误 {fileName}: {e.Message}");
            }
        }

        public T GetConfig<T>(int id) where T : class
        {
            if (_configCache.TryGetValue(typeof(T), out object dictObj) && dictObj is Dictionary<int, T> dict)
            {
                if (dict.TryGetValue(id, out T result)) return result;
            }
            return null;
        }

        public T GetConfig<T>(string id) where T : class
        {
            if (_configCache.TryGetValue(typeof(T), out object dictObj) && dictObj is Dictionary<string, T> dict)
            {
                if (dict.TryGetValue(id, out T result)) return result;
            }
            return null;
        }
        
        public Dictionary<int, T> GetDict<T>() where T : class
        {
            if (_configCache.TryGetValue(typeof(T), out object dictObj)) return dictObj as Dictionary<int, T>;
            return null;
        }
    }
}