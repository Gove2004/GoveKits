using System;
using System.IO;
using GoveKits.Binary;
using UnityEngine;

namespace GoveKits.Save
{
    public static class SaveManager
    {
        // 存档文件夹名称
        public static string SaveFolderName { get; set; } = "Saves";
        
        // 获取存档根目录 (持久化数据路径/Saves)
        public static string SaveFolderPath => Path.Combine(Application.persistentDataPath, SaveFolderName);

        /// <summary>
        /// 保存 IBinaryData 到文件 (原子操作：写入tmp -> 替换)
        /// </summary>
        public static bool SaveData(IBinaryData data, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || data == null) return false;

            try
            {
                // 1. 准备目录
                string fullPath = Path.Combine(SaveFolderPath, relativePath);
                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 2. 序列化数据
                // 直接使用数据长度分配 buffer，没有任何额外文件头
                int len = data.Length();
                byte[] buffer = new byte[len];
                int index = 0;
                
                data.Writing(buffer, ref index);

                // 3. 原子写入 (防止写入中途崩溃导致坏档)
                string tempPath = fullPath + ".tmp";
                File.WriteAllBytes(tempPath, buffer);

                // 删除旧文件并重命名新文件
                if (File.Exists(fullPath)) File.Delete(fullPath);
                File.Move(tempPath, fullPath);

#if UNITY_EDITOR
                DebugLogger.Log("SaveManager", $"Saved: {relativePath} ({len} bytes)");
#endif
                return true;
            }
            catch (Exception e)
            {
                DebugLogger.LogError("SaveManager", $"Save Failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载数据填充到现有实例 (复用对象)
        /// </summary>
        public static bool LoadData(IBinaryData data, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || data == null) return false;

            try
            {
                string fullPath = Path.Combine(SaveFolderPath, relativePath);
                if (!File.Exists(fullPath)) return false;

                byte[] buffer = File.ReadAllBytes(fullPath);
                if (buffer.Length == 0) return false;

                int index = 0;
                // 关键点：将文件总长度作为 endPos 传入，
                // 配合代码生成器中的 while(index < endPos) 逻辑
                data.Reading(buffer, ref index, buffer.Length);

                return true;
            }
            catch (Exception e)
            {
                DebugLogger.LogError("SaveManager", $"Load Failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 泛型加载：创建新实例并返回
        /// </summary>
        public static T LoadData<T>(string relativePath) where T : IBinaryData, new()
        {
            T instance = new T();
            if (LoadData(instance, relativePath))
            {
                return instance;
            }
            return default; // 加载失败返回 null
        }

        /// <summary>
        /// 尝试加载 (不抛异常，适合探测性读取)
        /// </summary>
        public static bool TryLoadData<T>(string relativePath, out T result) where T : IBinaryData, new()
        {
            if (!DataExists(relativePath))
            {
                result = default;
                return false;
            }

            result = new T();
            return LoadData(result, relativePath);
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
            // 清理可能残留的临时文件
            if (File.Exists(fullPath + ".tmp"))
            {
                File.Delete(fullPath + ".tmp");
            }

            return deleted;
        }

        /// <summary>
        /// 检查存档是否存在
        /// </summary>
        public static bool DataExists(string relativePath)
        {
            return File.Exists(Path.Combine(SaveFolderPath, relativePath));
        }

        /// <summary>
        /// 删除所有存档 (慎用)
        /// </summary>
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