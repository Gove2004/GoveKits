using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;


namespace GoveKits.Manager
{
    /// <summary>
    /// 存档数据基类, 支持版本控制
    /// </summary>
    public abstract class SaveData
    {
        [JsonProperty("version")] 
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// 分模块存档接口
    /// </summary>
    public interface ISaveModule<T> where T : SaveData
    {
        string ModuleName { get; }
        T GetSaveData();
        void SetLoadData(T data);
    }

    /// <summary>
    /// 分模块存档管理器
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        [Header("存档设置")]
        [SerializeField] private bool enableEncryption = true;
        [SerializeField] private string encryptionKey = "GoveKitsDefaultEncryptionKey123"; // 32字符密钥
        [SerializeField] private string saveFolder = "Saves";
        
        [Header("高级设置")]
        [SerializeField] private bool autoSaveOnQuit = true;
        [SerializeField] private float autoSaveInterval = 300f; // 自动保存间隔（秒）
        
        private Dictionary<string, ISaveModule<SaveData>> modules = new Dictionary<string, ISaveModule<SaveData>>();
        private float saveTimer;
        private JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,  // 美化输出
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,  // 忽略循环引用
            TypeNameHandling = TypeNameHandling.Auto  // 支持多态
        };
        private string saveFolderPath;

        protected void Awake()
        {
            InitializeSaveSystem();
        }

        private void InitializeSaveSystem()
        {
            // 创建存档文件夹
            saveFolderPath = Path.Combine(Application.persistentDataPath, saveFolder);
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
            }
            
            // 初始化加密提供程序
            if (enableEncryption)
            {
                ValidateEncryptionKey();
            }
        }

        private void Update()
        {
            // 自动保存计时器
            if (autoSaveInterval > 0)
            {
                saveTimer -= Time.deltaTime;
                if (saveTimer <= 0)
                {
                    SaveAllModules();
                    saveTimer = autoSaveInterval;
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnQuit)
            {
                SaveAllModules();
            }
        }

        #region 模块管理
        /// <summary>
        /// 注册存档模块
        /// </summary>
        public void RegisterModule<T>(ISaveModule<T> module) where T : SaveData
        {
            if (modules.ContainsKey(module.ModuleName))
            {
                Debug.LogWarning($"[SaveManager] 模块已注册: {module.ModuleName}");
                return;
            }
            
            // 使用类型转换注册
            modules[module.ModuleName] = new SaveModuleAdapter<T>(module);
            Debug.Log($"[SaveManager] 模块已注册: {module.ModuleName}");
            
            // 自动加载已有存档
            LoadModule(module.ModuleName);
        }

        /// <summary>
        /// 注销存档模块
        /// </summary>
        public void UnregisterModule(string moduleName)
        {
            if (modules.ContainsKey(moduleName))
            {
                modules.Remove(moduleName);
                Debug.Log($"[SaveManager] 模块已注销: {moduleName}");
            }
        }
        #endregion

        #region 保存与加载
        /// <summary>
        /// 保存指定模块
        /// </summary>
        public void SaveModule(string moduleName)
        {
            if (modules.TryGetValue(moduleName, out var module))
            {
                try
                {
                    var data = module.GetSaveData();
                    data.Version = Application.version; // 设置当前版本
                    
                    string json = JsonConvert.SerializeObject(data, jsonSettings);
                    json = enableEncryption ? Encrypt(json) : json;
                    
                    string path = GetModuleSavePath(moduleName);
                    File.WriteAllText(path, json);
                    
                    Debug.Log($"[SaveManager] 模块已保存: {moduleName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] 保存模块失败: {moduleName}\n{ex}");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveManager] 未找到模块: {moduleName}");
            }
        }

        /// <summary>
        /// 加载指定模块
        /// </summary>
        public void LoadModule(string moduleName)
        {
            if (modules.TryGetValue(moduleName, out var module))
            {
                string path = GetModuleSavePath(moduleName);
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        json = enableEncryption ? Decrypt(json) : json;
                        
                        // 动态反序列化
                        var data = JsonConvert.DeserializeObject<SaveData>(json, jsonSettings);
                        
                        // 版本检查
                        if (data.Version != Application.version)
                        {
                            Debug.LogWarning($"[SaveManager] 版本不匹配: {moduleName} " +
                                            $"(存档:{data.Version}, 当前:{Application.version})");
                            // 这里可以添加数据迁移逻辑
                        }
                        
                        module.SetLoadData(data);
                        Debug.Log($"[SaveManager] 模块已加载: {moduleName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveManager] 加载模块失败: {moduleName}\n{ex}");
                    }
                }
                else
                {
                    Debug.Log($"[SaveManager] 无存档文件: {moduleName}");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveManager] 未找到模块: {moduleName}");
            }
        }

        /// <summary>
        /// 保存所有模块
        /// </summary>
        public void SaveAllModules()
        {
            foreach (var moduleName in modules.Keys)
            {
                SaveModule(moduleName);
            }
            Debug.Log("[SaveManager] 所有模块已保存");
        }

        /// <summary>
        /// 加载所有模块
        /// </summary>
        public void LoadAllModules()
        {
            foreach (var moduleName in modules.Keys)
            {
                LoadModule(moduleName);
            }
            Debug.Log("[SaveManager] 所有模块已加载");
        }
        #endregion

        #region 加密方法
        private void ValidateEncryptionKey()
        {
            // 确保密钥长度正确
            if (encryptionKey.Length < 32)
            {
                encryptionKey = encryptionKey.PadRight(32, '0');
            }
            else if (encryptionKey.Length > 32)
            {
                encryptionKey = encryptionKey.Substring(0, 32);
            }
        }

        private string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
                aesAlg.GenerateIV(); // 生成随机IV

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // 先写入IV
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    
                    // 再写入加密数据
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            byte[] fullCipher = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
                
                // 提取IV（前16字节）
                byte[] iv = new byte[16];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;
                
                // 提取加密数据
                byte[] cipherData = new byte[fullCipher.Length - iv.Length];
                Buffer.BlockCopy(fullCipher, iv.Length, cipherData, 0, cipherData.Length);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherData))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取模块存档路径
        /// </summary>
        private string GetModuleSavePath(string moduleName)
        {
            return Path.Combine(saveFolderPath, $"{moduleName}.save");
        }

        /// <summary>
        /// 获取所有已注册模块名
        /// </summary>
        public List<string> GetAllModuleNames()
        {
            return new List<string>(modules.Keys);
        }

        /// <summary>
        /// 检查模块是否有存档
        /// </summary>
        public bool HasSaveForModule(string moduleName)
        {
            return File.Exists(GetModuleSavePath(moduleName));
        }

        /// <summary>
        /// 删除模块存档
        /// </summary>
        public void DeleteModuleSave(string moduleName)
        {
            string path = GetModuleSavePath(moduleName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveManager] 已删除存档: {moduleName}");
            }
        }
        #endregion

        #region 适配器类（处理泛型接口）
        /// <summary>
        /// 适配器类，用于处理泛型接口的存储
        /// </summary>
        private class SaveModuleAdapter<T> : ISaveModule<SaveData> where T : SaveData
        {
            private readonly ISaveModule<T> _module;
            
            public string ModuleName => _module.ModuleName;
            
            public SaveModuleAdapter(ISaveModule<T> module)
            {
                _module = module;
            }
            
            public SaveData GetSaveData()
            {
                return _module.GetSaveData();
            }
            
            public void SetLoadData(SaveData data)
            {
                if (data is T typedData)
                {
                    _module.SetLoadData(typedData);
                }
                else
                {
                    Debug.LogError($"[SaveManager] 类型不匹配: {ModuleName}");
                }
            }
        }
        #endregion
    }
}