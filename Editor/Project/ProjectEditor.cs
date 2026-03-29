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
        private string _androidPrivacySourcePath = "Assets/GoveKits/Plugins/Android Privacy";
        private string _androidPrivacyTargetPath = "Assets/Plugins/Android";
        
        /// <summary>
        /// 打开 Project 面板。
        /// </summary>
        [MenuItem("GoveKits/Project")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectEditor>("Project Editor");
            window.minSize = new Vector2(420, 260);
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

            EditorGUILayout.Space(14);
            EditorGUILayout.LabelField("Android 隐私弹窗", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("源目录:", GUILayout.Width(80));
            _androidPrivacySourcePath = EditorGUILayout.TextField(_androidPrivacySourcePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择 Android 隐私弹窗源目录",
                    Path.GetDirectoryName(ToAbsolutePath(_androidPrivacySourcePath)), string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    _androidPrivacySourcePath = ToProjectRelativeOrAbsolute(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标目录:", GUILayout.Width(80));
            _androidPrivacyTargetPath = EditorGUILayout.TextField(_androidPrivacyTargetPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择 Android 隐私弹窗目标目录",
                    Path.GetDirectoryName(ToAbsolutePath(_androidPrivacyTargetPath)), string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    _androidPrivacyTargetPath = ToProjectRelativeOrAbsolute(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            string androidPrivacySourceAbsolute = ToAbsolutePath(_androidPrivacySourcePath);
            if (!Directory.Exists(androidPrivacySourceAbsolute))
            {
                EditorGUILayout.HelpBox($"源目录不存在: {androidPrivacySourceAbsolute}", MessageType.Warning);
            }

            GUI.enabled = Directory.Exists(androidPrivacySourceAbsolute);
            if (GUILayout.Button("一键复制安卓隐私弹窗", GUILayout.Height(30)))
            {
                CopyAndroidPrivacyPopup();
            }
            GUI.enabled = true;
        }
        
        /// <summary>
        /// 根据模板生成 .gitignore 文件到项目根目录。
        /// </summary>
        private void CreateGitIgnore()
        {
            string templatePath = ToAbsolutePath(_gitignorePath);
            if (!File.Exists(templatePath))
            {
                ShowNotification(new GUIContent("模板文件不存在"));
                LogCore.LogError("ProjectEditor", $".gitignore 模板不存在: {templatePath}");
                return;
            }

            string fullPath = Path.Combine(ProjectRootPath, ".gitignore");
            if (File.Exists(fullPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "覆盖确认",
                    $"检测到已存在 .gitignore，是否覆盖？\n{fullPath}",
                    "覆盖",
                    "取消");
                if (!overwrite)
                {
                    return;
                }
            }

            try
            {
                string gitignoreContent = File.ReadAllText(templatePath, Encoding.UTF8);
                File.WriteAllText(fullPath, gitignoreContent, Encoding.UTF8);

                ShowNotification(new GUIContent("已创建 .gitignore 文件"));
                LogCore.LogGreen("ProjectEditor", $"已创建 .gitignore 文件: {fullPath}");
            }
            catch (Exception e)
            {
                ShowNotification(new GUIContent("创建 .gitignore 失败"));
                LogCore.LogError("ProjectEditor", $"创建 .gitignore 失败: {e.Message}");
            }
        }

        private void CopyAndroidPrivacyPopup()
        {
            string sourceRoot = NormalizeFullPath(ToAbsolutePath(_androidPrivacySourcePath));
            string targetRoot = NormalizeFullPath(ToAbsolutePath(_androidPrivacyTargetPath));

            if (!Directory.Exists(sourceRoot))
            {
                ShowNotification(new GUIContent("源目录不存在"));
                LogCore.LogError("ProjectEditor", $"Android 隐私弹窗源目录不存在: {sourceRoot}");
                return;
            }

            if (string.IsNullOrWhiteSpace(targetRoot))
            {
                ShowNotification(new GUIContent("目标目录无效"));
                LogCore.LogError("ProjectEditor", "Android 隐私弹窗目标目录无效");
                return;
            }

            if (IsSameOrSubPath(sourceRoot, targetRoot) || IsSameOrSubPath(targetRoot, sourceRoot))
            {
                ShowNotification(new GUIContent("源目录与目标目录存在包含关系"));
                LogCore.LogError("ProjectEditor", $"源目标目录冲突: {sourceRoot} <-> {targetRoot}");
                return;
            }

            if (Directory.Exists(targetRoot))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "覆盖确认",
                    $"目标目录已存在，复制将覆盖同名文件。\n{targetRoot}",
                    "继续",
                    "取消");
                if (!overwrite)
                {
                    return;
                }
            }

            try
            {
                Directory.CreateDirectory(targetRoot);
                int copiedFileCount = CopyDirectoryWithoutMeta(sourceRoot, targetRoot);
                AssetDatabase.Refresh();

                ShowNotification(new GUIContent($"复制完成: {copiedFileCount} 个文件"));
                LogCore.LogGreen("ProjectEditor", $"安卓隐私弹窗复制完成({copiedFileCount}): {sourceRoot} -> {targetRoot}");
            }
            catch (Exception e)
            {
                ShowNotification(new GUIContent("复制失败"));
                LogCore.LogError("ProjectEditor", $"安卓隐私弹窗复制失败: {e.Message}");
            }
        }

        private static int CopyDirectoryWithoutMeta(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            int copiedFileCount = 0;

            string[] files = Directory.GetFiles(sourceDir);
            foreach (string file in files)
            {
                if (string.Equals(Path.GetExtension(file), ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileName = Path.GetFileName(file);
                string targetFile = Path.Combine(targetDir, fileName);
                File.Copy(file, targetFile, true);
                copiedFileCount++;
            }

            string[] directories = Directory.GetDirectories(sourceDir);
            foreach (string directory in directories)
            {
                string directoryName = Path.GetFileName(directory);
                if (string.IsNullOrEmpty(directoryName))
                {
                    continue;
                }

                string targetSubDir = Path.Combine(targetDir, directoryName);
                copiedFileCount += CopyDirectoryWithoutMeta(directory, targetSubDir);
            }

            return copiedFileCount;
        }
        
        private void OnEnable()
        {
            _gitignorePath = EditorPrefs.GetString("ProjectEditor.GitIgnorePath", _gitignorePath);
            _androidPrivacySourcePath = EditorPrefs.GetString("ProjectEditor.AndroidPrivacySourcePath", _androidPrivacySourcePath);
            _androidPrivacyTargetPath = EditorPrefs.GetString("ProjectEditor.AndroidPrivacyTargetPath", _androidPrivacyTargetPath);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("ProjectEditor.GitIgnorePath", _gitignorePath);
            EditorPrefs.SetString("ProjectEditor.AndroidPrivacySourcePath", _androidPrivacySourcePath);
            EditorPrefs.SetString("ProjectEditor.AndroidPrivacyTargetPath", _androidPrivacyTargetPath);
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

        private static bool IsSameOrSubPath(string parentPath, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(candidatePath))
            {
                return false;
            }

            string parent = NormalizeFullPath(parentPath).TrimEnd('/');
            string candidate = NormalizeFullPath(candidatePath).TrimEnd('/');
            return string.Equals(parent, candidate, StringComparison.OrdinalIgnoreCase)
                || candidate.StartsWith(parent + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.GetFullPath(path).Replace('\\', '/');
        }
    }
}