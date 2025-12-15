using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GoveKits.Binary;

namespace GoveKits.Editor
{
    /// <summary>
    /// 编辑器入口：扫描程序集并调用 Builder 生成文件。
    /// </summary>
    public static class BinaryDataGenerator
    {
        [MenuItem("GoveKits/Code/Generate Binary Code")]
        public static void Generate()
        {
            // --- 优化1: 智能过滤程序集 ---
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !IsSystemAssembly(a.GetName().Name));

            // 获取所有标记了 GenBinaryData 的类
            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsDefined(typeof(GenBinaryDataAttribute), false))
                .ToList();

            if (types.Count == 0)
            {
                LogManager.Log("BinaryDataGenerator", "No types found with [GenBinaryData].");
                return;
            }

            int updateCount = 0;
            
            try
            {
                for (int i = 0; i < types.Count; i++)
                {
                    var type = types[i];
                    
                    // --- 优化2: 进度条显示 ---
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Generating Binary Code", 
                        $"Processing {type.Name}...", 
                        (float)i / types.Count))
                    {
                        LogManager.LogWarning("BinaryDataGenerator", "Operation cancelled.");
                        break;
                    }

                    if (ProcessType(type))
                    {
                        updateCount++;
                    }
                }
            }
            catch (Exception e)
            {
                LogManager.LogError("BinaryDataGenerator", $"Fatal Error: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (updateCount > 0)
            {
                AssetDatabase.Refresh();
                LogManager.Log("BinaryDataGenerator", $"<color=green>Updated {updateCount} files.</color> ({types.Count - updateCount} skipped)");
            }
            else
            {
                LogManager.Log("BinaryDataGenerator", "All files are up to date.");
            }
        }

        private static bool ProcessType(Type type)
        {
            // 1. 获取保存路径
            var attr = type.GetCustomAttribute<GenBinaryDataAttribute>(false);
            string relativePath = string.IsNullOrEmpty(attr.SavePath) 
                ? GenBinaryDataAttribute.DefaultSavePath 
                : attr.SavePath;

            // --- 优化3: 健壮的路径拼接 ---
            string fullDir = GetFullPath(relativePath);
            if (!Directory.Exists(fullDir)) Directory.CreateDirectory(fullDir);

            // 2. 调用核心生成器构建代码
            string newContent = BinaryCodeBuilder.Build(type);
            string fullPath = Path.Combine(fullDir, $"{type.Name}.Gen.cs");

            // --- 优化4: 智能写入 (对比内容) ---
            if (File.Exists(fullPath))
            {
                string oldContent = File.ReadAllText(fullPath);
                // 简单对比长度和内容，避免不必要的 IO 和 Unity 编译
                if (oldContent == newContent) return false; 
            }

            File.WriteAllText(fullPath, newContent);
            return true;
        }

        private static bool IsSystemAssembly(string name)
        {
            return name.StartsWith("Unity") || 
                   name.StartsWith("System") || 
                   name.StartsWith("Microsoft") || 
                   name.StartsWith("mscorlib") ||
                   name.StartsWith("nunit");
        }

        private static string GetFullPath(string relativePath)
        {
            // 处理以 "Assets" 开头的路径 vs 纯相对路径
            if (relativePath.StartsWith("Assets"))
            {
                // Application.dataPath 指向 Assets 文件夹
                // Directory.GetParent 获取项目根目录
                return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
            }
            else
            {
                // 默认在 Assets 下
                return Path.Combine(Application.dataPath, relativePath);
            }
        }
    }
}