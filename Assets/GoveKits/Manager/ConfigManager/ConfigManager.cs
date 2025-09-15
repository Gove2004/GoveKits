using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using System;
using System.IO;


namespace GoveKits.Manager
{
    /// <summary>
    /// 基础配置类, DTO
    /// </summary>
    public class DataConfig
    {
        // 可根据需要添加配置通用属性
    }

    /// <summary>
    /// 配置管理器
    /// </summary>
    public class ConfigManager : MonoSingleton<ConfigManager>
    {
        [SerializeField] private string _configPath = "Assets/Config/Json"; // 配置文件目录
        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,  // 忽略缺失成员
            NullValueHandling = NullValueHandling.Ignore  // 忽略null值
        };
        private readonly Dictionary<string, JToken> _jsonDict = new Dictionary<string, JToken>();

        public Action OnConfigAllLoaded; // 配置加载完成回调

        /// <summary>
        /// 获取配置对象
        /// </summary>
        /// <typeparam name="T">目标DTO类型</typeparam>
        /// <param name="path">配置路径，格式: "文件名.JSON路径"，如 "player.health" 或 "language.texts[0]"</param>
        public T GetConfig<T>(string path)
        {
            try
            {
                // 分割路径，第一部分是文件名
                string[] pathParts = path.Split('.');
                if (pathParts.Length == 0)
                {
                    Debug.LogWarning($"[ConfigManager] 无效路径: {path}");
                    return default(T);
                }

                string fileName = pathParts[0];

                if (!_jsonDict.TryGetValue(fileName, out JToken jsonToken))
                {
                    Debug.LogWarning($"[ConfigManager] 未找到配置文件: {fileName}");
                    return default(T);
                }

                JToken targetToken;

                if (pathParts.Length == 1)
                {
                    // 只有文件名，返回整个JSON对象
                    targetToken = jsonToken;
                }
                else
                {
                    // 导航到目标节点
                    targetToken = NavigateToToken(jsonToken, pathParts, 1);

                    if (targetToken == null)
                    {
                        Debug.LogWarning($"[ConfigManager] 路径不存在: {path}");
                        return default(T);
                    }
                }

                // 反序列化为目标类型
                T result = targetToken.ToObject<T>(JsonSerializer.Create(jsonSettings));

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager] 获取配置失败: {path}, 错误: {e.Message}");
                return default(T);
            }
        }
        
        /// <summary>
        /// 导航到指定的JSON Token
        /// </summary>
        /// <param name="currentToken">当前Token</param>
        /// <param name="pathParts">路径分段</param>
        /// <param name="startIndex">开始索引</param>
        /// <returns>目标Token</returns>
        private JToken NavigateToToken(JToken currentToken, string[] pathParts, int startIndex)
        {
            for (int i = startIndex; i < pathParts.Length; i++)
            {
                string part = pathParts[i];
                
                // 处理数组索引，如 "texts[0]"
                if (part.Contains('[') && part.Contains(']'))
                {
                    string propertyName = part.Substring(0, part.IndexOf('['));
                    string indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);
                    
                    if (int.TryParse(indexStr, out int index))
                    {
                        currentToken = currentToken[propertyName]?[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // 普通属性访问
                    currentToken = currentToken[part];
                }
                
                if (currentToken == null)
                {
                    return null;
                }
            }
            
            return currentToken;
        }
        
        /// <summary>
        /// 游戏开始时加载所有配置文件
        /// </summary>
        public void LoadAllConfigs()
        {
            _jsonDict.Clear();
            
            if (!Directory.Exists(_configPath))
            {
                Debug.LogWarning($"[ConfigManager] 配置目录不存在: {_configPath}");
                return;
            }
            
            // 直接加载所有JSON文件
            LoadJsonFilesRecursive(_configPath);
            
            OnConfigAllLoaded?.Invoke();

            Debug.Log($"[ConfigManager] 配置加载完成，总共加载了 {_jsonDict.Count} 个文件");
        }
        
        /// <summary>
        /// 递归加载JSON文件
        /// </summary>
        /// <param name="directory">目录路径</param>
        private void LoadJsonFilesRecursive(string directory)
        {
            // 获取当前目录的所有JSON文件
            string[] jsonFiles = Directory.GetFiles(directory, "*.json");
            
            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string jsonContent = File.ReadAllText(filePath);
                    JToken jsonToken = JToken.Parse(jsonContent);
                    
                    _jsonDict[fileName] = jsonToken;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ConfigManager] 加载失败: {filePath}, 错误: {e.Message}");
                }
            }
            
            // 递归处理子目录
            string[] subDirectories = Directory.GetDirectories(directory);
            foreach (string subDir in subDirectories)
            {
                LoadJsonFilesRecursive(subDir);
            }
        }
        
        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        /// <param name="path">配置路径</param>
        /// <returns>是否存在</returns>
        public bool HasConfig(string path)
        {
            if (path == null) return false;
            try
            {
                string[] pathParts = path.Split('.');
                string fileName = pathParts[0];

                if (!_jsonDict.ContainsKey(fileName))
                    return false;

                if (pathParts.Length == 1)
                    return true;

                JToken targetToken = NavigateToToken(_jsonDict[fileName], pathParts, 1);
                return targetToken != null;
            }
            catch
            {
                Debug.LogWarning($"[ConfigManager] 检查配置失败: {path}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取所有配置文件名
        /// </summary>
        /// <returns>文件名数组</returns>
        public string[] GetAllConfigNames()
        {
            string[] names = new string[_jsonDict.Count];
            _jsonDict.Keys.CopyTo(names, 0);
            return names;
        }
        
        /// <summary>
        /// 重新加载指定配置文件
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        public void ReloadConfig(string fileName)
        {
            string filePath = Path.Combine(_configPath, fileName + ".json");
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[ConfigManager] 文件不存在: {filePath}");
                return;
            }
            
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                JToken jsonToken = JToken.Parse(jsonContent);
                _jsonDict[fileName] = jsonToken;
                
                Debug.Log($"[ConfigManager] 重新加载配置: {fileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager] 重新加载失败: {filePath}, 错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 初始化时自动加载
        /// </summary>
        public void Awake()
        {
            if (Instance == this && Application.isPlaying)
            {
                LoadAllConfigs();
            }
        }


    }
}