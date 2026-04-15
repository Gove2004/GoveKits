using System;
using System.IO;
using System.Text;
using GoveKits.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class ProjectWindow : EditorWindow
    {
        private string _gitignorePath = "Assets/GoveKits/Editor/Project/gitignore.txt";

        [MenuItem("GoveKits/Project", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectWindow>("Project 工具");
            window.minSize = new Vector2(450, 250);
            window.Show();
        }

        private void OnEnable()
        {
            _gitignorePath = EditorPrefs.GetString("GoveKits_Project_GitIgnorePath", _gitignorePath);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("GoveKits_Project_GitIgnorePath", _gitignorePath);
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawConfigSection();
            DrawActionSection();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("项目基础环境初始化工具", EditorStyles.largeLabel);
            GUILayout.Space(5);
            DrawLine();
            GUILayout.Space(10);
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("配置 .gitignore 模板", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 80;
            _gitignorePath = EditorGUILayout.TextField("模板文件:", _gitignorePath);
            EditorGUIUtility.labelWidth = 0;

            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择 .gitignore 模板文件", 
                    Path.GetDirectoryName(GoveKitsPathUtility.ToAbsolutePath(_gitignorePath)), "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    _gitignorePath = GoveKitsPathUtility.ToProjectRelativeOrAbsolute(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            string absolutePath = GoveKitsPathUtility.ToAbsolutePath(_gitignorePath);
            if (!File.Exists(absolutePath))
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox($"模板文件不存在，请重新选择。\n当前路径: {absolutePath}", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(15);
        }

        private void DrawActionSection()
        {
            DrawLine();
            GUILayout.Space(10);

            string absolutePath = GoveKitsPathUtility.ToAbsolutePath(_gitignorePath);
            GUI.enabled = File.Exists(absolutePath);

            if (GUILayout.Button("初始化项目 .gitignore", GUILayout.Height(30)))
            {
                CreateGitIgnore();
            }
            
            GUI.enabled = true;
        }

        private void CreateGitIgnore()
        {
            string templatePath = GoveKitsPathUtility.ToAbsolutePath(_gitignorePath);
            string fullPath = Path.Combine(GoveKitsPathUtility.ProjectRootPath, ".gitignore");

            if (File.Exists(fullPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "覆盖确认",
                    $"检测到项目根目录已存在 .gitignore，是否覆盖？\n{fullPath}",
                    "覆盖",
                    "取消");
                if (!overwrite) return;
            }

            try
            {
                string gitignoreContent = File.ReadAllText(templatePath, Encoding.UTF8);
                File.WriteAllText(fullPath, gitignoreContent, Encoding.UTF8);

                ShowNotification(new GUIContent("已成功创建 .gitignore 文件"));
                LogCore.Success("ProjectEditor", $"已创建 .gitignore 文件: {fullPath}");
            }
            catch (Exception e)
            {
                ShowNotification(new GUIContent("创建 .gitignore 失败"));
                LogCore.Error("ProjectEditor", $"创建 .gitignore 失败: {e.Message}");
            }
        }

        private void DrawLine(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }

    /// <summary>
    /// GoveKits 全局 Editor 路径工具类
    /// </summary>
    internal static class GoveKitsPathUtility
    {
        public static string ProjectRootPath
        {
            get
            {
                DirectoryInfo info = Directory.GetParent(Application.dataPath);
                return info != null ? info.FullName : Application.dataPath;
            }
        }

        public static string ToAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            if (Path.IsPathRooted(path)) return path;

            string normalized = path.Replace('\\', '/');
            return Path.Combine(ProjectRootPath, normalized);
        }

        public static string ToProjectRelativeOrAbsolute(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath)) return string.Empty;

            string full = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string root = ProjectRootPath.Replace('\\', '/').TrimEnd('/');
            
            if (full.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                return full.Substring(root.Length + 1);
            }
            return absolutePath;
        }

        public static bool IsSameOrSubPath(string parentPath, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(candidatePath)) return false;

            string parent = NormalizeFullPath(parentPath).TrimEnd('/');
            string candidate = NormalizeFullPath(candidatePath).TrimEnd('/');
            
            return string.Equals(parent, candidate, StringComparison.OrdinalIgnoreCase) || 
                   candidate.StartsWith(parent + "/", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeFullPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path).Replace('\\', '/');
        }
    }
}