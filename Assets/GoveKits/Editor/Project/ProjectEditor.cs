using System;
using System.IO;
using System.Text;
using GoveKits.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    /// <summary>
    /// 项目辅助面板：仅用于初始化 .gitignore。
    /// </summary>
    public class ProjectEditor : EditorWindow
    {
        private string _gitignorePath = "Assets/GoveKits/Editor/Project/gitignore.txt";
        
        /// <summary>
        /// 打开 Project 面板。
        /// </summary>
        [MenuItem("GoveKits/Project")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectEditor>("Project Editor");
            window.minSize = new Vector2(360, 120);
            window.Show();
        }

        /// <summary>
        /// 面板 UI 绘制入口。
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(".gitignore", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("模板文件:", GUILayout.Width(80));
            _gitignorePath = EditorGUILayout.TextField(_gitignorePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择 .gitignore 模板文件", 
                    Path.GetDirectoryName(ToAbsolutePath(_gitignorePath)), "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    _gitignorePath = ToProjectRelativeOrAbsolute(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            string gitIgnoreTemplateAbsolute = ToAbsolutePath(_gitignorePath);
            if (!File.Exists(gitIgnoreTemplateAbsolute))
            {
                EditorGUILayout.HelpBox($"模板文件不存在: {gitIgnoreTemplateAbsolute}", MessageType.Warning);
            }
            
            GUI.enabled = File.Exists(gitIgnoreTemplateAbsolute);
            if (GUILayout.Button("初始化 .gitignore", GUILayout.Height(30)))
            {
                CreateGitIgnore();
            }
            GUI.enabled = true;
        }
        
        /// <summary>
        /// 根据模板生成 .gitignore 文件到项目根目录。
        /// </summary>
        private void CreateGitIgnore()
        {
            string gitignoreContent = File.ReadAllText(ToAbsolutePath(_gitignorePath), Encoding.UTF8);
            string fullPath = Path.Combine(ProjectRootPath, ".gitignore");
            File.WriteAllText(fullPath, gitignoreContent, Encoding.UTF8);
            
            ShowNotification(new GUIContent("已创建 .gitignore 文件"));
            LogCore.LogGreen("ProjectEditor", "已创建 .gitignore 文件");
        }
        
        private void OnEnable()
        {
            const string key = "ProjectEditor.GitIgnorePath";
            _gitignorePath = EditorPrefs.GetString(key, _gitignorePath);
        }

        private void OnDisable()
        {
            const string key = "ProjectEditor.GitIgnorePath";
            EditorPrefs.SetString(key, _gitignorePath);
        }

        private static string ProjectRootPath
        {
            get
            {
                DirectoryInfo info = Directory.GetParent(Application.dataPath);
                return info != null ? info.FullName : Application.dataPath;
            }
        }

        private static string ToAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(ProjectRootPath, normalized);
            }

            return Path.Combine(ProjectRootPath, normalized);
        }

        private static string ToProjectRelativeOrAbsolute(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return string.Empty;
            }

            string full = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string root = ProjectRootPath.Replace('\\', '/').TrimEnd('/');
            if (full.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                return full.Substring(root.Length + 1);
            }

            return absolutePath;
        }
    }
}