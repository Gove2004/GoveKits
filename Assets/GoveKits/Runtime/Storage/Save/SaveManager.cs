using System;
using System.IO;
using Google.Protobuf; // 关键引用
using UnityEngine;

namespace GoveKits.Save
{
    public static class SaveManager
    {
        // 存档文件夹名称
        public static string SaveFolderName { get; set; } = "Saves";
        
        // 获取存档根目录
        public static string SaveFolderPath => Path.Combine(Application.persistentDataPath, SaveFolderName);

        // 静态构造函数确保目录存在
        static SaveManager()
        {
            if (!Directory.Exists(SaveFolderPath))
            {
                Directory.CreateDirectory(SaveFolderPath);
            }
        }

        /// <summary>
        /// 保存 Protobuf 数据到文件 (原子操作)
        /// </summary>
        public static bool SaveData(IMessage data, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || data == null) return false;

            try
            {
                string fullPath = Path.Combine(SaveFolderPath, relativePath);
                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string tempPath = fullPath + ".tmp";

                // === 核心替换：使用 Protobuf 原生序列化 ===
                using (var output = File.Create(tempPath))
                {
                    data.WriteTo(output);
                }

                // 原子替换
                if (File.Exists(fullPath)) File.Delete(fullPath);
                File.Move(tempPath, fullPath);

#if UNITY_EDITOR
                LogManager.LogGreen("SaveManager", $"Saved: {relativePath}");
#endif
                return true;
            }
            catch (Exception e)
            {
                LogManager.LogError("SaveManager", $"Save Failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载数据并合并到现有实例
        /// </summary>
        public static bool LoadData(IMessage data, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || data == null) return false;

            try
            {
                string fullPath = Path.Combine(SaveFolderPath, relativePath);
                if (!File.Exists(fullPath)) return false;

                // === 核心替换：使用 Protobuf 原生合并 ===
                using (var input = File.OpenRead(fullPath))
                {
                    data.MergeFrom(input); // MergeFrom 会将文件数据填充到 data 对象中
                }

                return true;
            }
            catch (Exception e)
            {
                LogManager.LogError("SaveManager", $"Load Failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 泛型加载：创建新实例并返回
        /// </summary>
        /// <typeparam name="T">必须是 Protobuf 生成的类</typeparam>
        public static T LoadData<T>(string relativePath) where T : IMessage<T>, new()
        {
            // 如果文件不存在，直接返回默认的新对象，而不是 null (防止空引用)
            if (!DataExists(relativePath)) return new T();

            try
            {
                string fullPath = Path.Combine(SaveFolderPath, relativePath);
                using (var input = File.OpenRead(fullPath))
                {
                    // 使用 MessageParser 解析
                    var parser = new MessageParser<T>(() => new T());
                    return parser.ParseFrom(input);
                }
            }
            catch (Exception e)
            {
                LogManager.LogError("SaveManager", $"Load<T> Failed: {e.Message}");
                return new T(); // 出错返回空对象
            }
        }

        /// <summary>
        /// 删除特定存档
        /// </summary>
        public static bool DeleteData(string relativePath)
        {
            string fullPath = Path.Combine(SaveFolderPath, relativePath);
            bool deleted = false;

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                deleted = true;
            }
            if (File.Exists(fullPath + ".tmp")) File.Delete(fullPath + ".tmp");

            return deleted;
        }

        public static bool DataExists(string relativePath)
        {
            return File.Exists(Path.Combine(SaveFolderPath, relativePath));
        }

        public static void DeleteAllDatas()
        {
            if (Directory.Exists(SaveFolderPath))
            {
                Directory.Delete(SaveFolderPath, true);
                Directory.CreateDirectory(SaveFolderPath);
            }
        }
    }
}