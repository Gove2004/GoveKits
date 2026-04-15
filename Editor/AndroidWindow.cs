using System;
using System.IO;
using GoveKits.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class AndroidWindow : EditorWindow
    {
        private string _androidPrivacySourcePath = "Assets/GoveKits/Plugins/Android Privacy";
        private string _androidPrivacyTargetPath = "Assets/Plugins/Android";
        private Vector2 _scrollPos;

        [MenuItem("GoveKits/Android", false, 11)]
        public static void ShowWindow()
        {
            var window = GetWindow<AndroidWindow>("Android 配置");
            window.minSize = new Vector2(450, 300);
            window.Show();
        }

        private void OnEnable()
        {
            _androidPrivacySourcePath = EditorPrefs.GetString("GoveKits_Android_PrivacySource", _androidPrivacySourcePath);
            _androidPrivacyTargetPath = EditorPrefs.GetString("GoveKits_Android_PrivacyTarget", _androidPrivacyTargetPath);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("GoveKits_Android_PrivacySource", _androidPrivacySourcePath);
            EditorPrefs.SetString("GoveKits_Android_PrivacyTarget", _androidPrivacyTargetPath);
        }

        private void OnGUI()
        {
            DrawHeader();
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawConfigSection();
            EditorGUILayout.EndScrollView();

            DrawActionSection();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Android 隐私弹窗部署工具", EditorStyles.largeLabel);
            GUILayout.Space(5);
            DrawLine();
            GUILayout.Space(10);
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("目录配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");

            EditorGUIUtility.labelWidth = 80;

            // 1. 源目录
            EditorGUILayout.BeginHorizontal();
            _androidPrivacySourcePath = EditorGUILayout.TextField("源目录:", _androidPrivacySourcePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择 Android 隐私弹窗源目录",
                    Path.GetDirectoryName(GoveKitsPathUtility.ToAbsolutePath(_androidPrivacySourcePath)), string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    _androidPrivacySourcePath = GoveKitsPathUtility.ToProjectRelativeOrAbsolute(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 2. 目标目录
            EditorGUILayout.BeginHorizontal();
            _androidPrivacyTargetPath = EditorGUILayout.TextField("目标目录:", _androidPrivacyTargetPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择 Android 隐私弹窗目标目录",
                    Path.GetDirectoryName(GoveKitsPathUtility.ToAbsolutePath(_androidPrivacyTargetPath)), string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    _androidPrivacyTargetPath = GoveKitsPathUtility.ToProjectRelativeOrAbsolute(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 0; // 恢复默认
            EditorGUILayout.EndVertical();

            // 3. 错误校验提示
            GUILayout.Space(10);
            string sourceAbsolute = GoveKitsPathUtility.ToAbsolutePath(_androidPrivacySourcePath);
            if (!Directory.Exists(sourceAbsolute))
            {
                EditorGUILayout.HelpBox($"源目录不存在，请检查路径！\n当前路径: {sourceAbsolute}", MessageType.Error);
            }
        }

        private void DrawActionSection()
        {
            DrawLine();
            GUILayout.Space(10);

            string sourceAbsolute = GoveKitsPathUtility.ToAbsolutePath(_androidPrivacySourcePath);
            GUI.enabled = Directory.Exists(sourceAbsolute);

            if (GUILayout.Button("一键部署 Android 隐私弹窗", GUILayout.Height(30)))
            {
                CopyAndroidPrivacyPopup();
            }

            GUI.enabled = true;
            GUILayout.Space(10);
        }

        private void CopyAndroidPrivacyPopup()
        {
            string sourceRoot = GoveKitsPathUtility.NormalizeFullPath(GoveKitsPathUtility.ToAbsolutePath(_androidPrivacySourcePath));
            string targetRoot = GoveKitsPathUtility.NormalizeFullPath(GoveKitsPathUtility.ToAbsolutePath(_androidPrivacyTargetPath));

            if (string.IsNullOrWhiteSpace(targetRoot))
            {
                ShowNotification(new GUIContent("目标目录无效"));
                LogCore.Error("AndroidEditor", "Android 隐私弹窗目标目录无效");
                return;
            }

            if (GoveKitsPathUtility.IsSameOrSubPath(sourceRoot, targetRoot) || GoveKitsPathUtility.IsSameOrSubPath(targetRoot, sourceRoot))
            {
                ShowNotification(new GUIContent("源目录与目标目录存在冲突"));
                LogCore.Error("AndroidEditor", $"源目录与目标目录存在包含关系: {sourceRoot} <-> {targetRoot}");
                return;
            }

            if (Directory.Exists(targetRoot))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "覆盖确认",
                    $"目标目录已存在，复制操作将覆盖其中的同名文件。\n\n路径：{targetRoot}\n\n是否继续？",
                    "继续覆盖",
                    "取消");
                if (!overwrite) return;
            }

            try
            {
                Directory.CreateDirectory(targetRoot);
                int copiedFileCount = CopyDirectoryWithoutMeta(sourceRoot, targetRoot);
                AssetDatabase.Refresh();

                ShowNotification(new GUIContent($"部署成功: 共复制 {copiedFileCount} 个文件"));
                LogCore.Success("AndroidEditor", $"安卓隐私弹窗部署完成 ({copiedFileCount} files): {sourceRoot} -> {targetRoot}");
            }
            catch (Exception e)
            {
                ShowNotification(new GUIContent("复制失败，详情见控制台"));
                LogCore.Error("AndroidEditor", $"安卓隐私弹窗复制失败: {e.Message}");
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
                if (string.IsNullOrEmpty(directoryName)) continue;

                string targetSubDir = Path.Combine(targetDir, directoryName);
                copiedFileCount += CopyDirectoryWithoutMeta(directory, targetSubDir);
            }

            return copiedFileCount;
        }

        private void DrawLine(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
}