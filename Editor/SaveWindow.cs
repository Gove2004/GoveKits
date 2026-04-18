using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using GoveKits.Runtime.Storage;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class SaveWindow : EditorWindow
    {
        private enum PreviewMode
        {
            Auto,
            Utf8,
            Hex,
        }

        private const int PreviewMaxBytes = 256 * 1024; // 防止过大文件卡死编辑器
        private readonly HashSet<string> _expandedDirectories = new();

        private Vector2 _treeScroll;
        private Vector2 _previewScroll;

        private string _selectedFilePath;
        private byte[] _selectedBytes;
        private bool _selectedBytesTruncated;
        private PreviewMode _previewMode = PreviewMode.Auto;

        [MenuItem("GoveKits/Save", false, 201)]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveWindow>("Save 浏览器");
            window.minSize = new Vector2(700, 500); // 左右分栏需要宽一点
            window.Show();
        }

        /// <summary>
        /// 黑科技：反射获取 SaveCore 的私有静态变量 _rootPath
        /// </summary>
        private string GetActualSaveRootPath()
        {
            try
            {
                // 反射拿 _rootPath
                FieldInfo fieldInfo = typeof(SaveCore).GetField("_rootPath", BindingFlags.NonPublic | BindingFlags.Static);
                if (fieldInfo != null)
                {
                    string path = fieldInfo.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(path)) return path;
                }
            }
            catch { /* 忽略反射异常 */ }

            // 如果框架还没初始化，给个默认兜底路径
            return Path.Combine(Application.persistentDataPath, "Saves");
        }

        private void OnEnable()
        {
            string rootPath = GetActualSaveRootPath();
            EnsureFolder(rootPath);
            _expandedDirectories.Add(rootPath);
        }

        private void OnGUI()
        {
            string rootPath = GetActualSaveRootPath();
            DrawHeader(rootPath);

            if (!Directory.Exists(rootPath))
            {
                EditorGUILayout.HelpBox($"存档根目录不存在：{rootPath}", MessageType.Warning);
                if (GUILayout.Button("创建目录", GUILayout.Height(30))) EnsureFolder(rootPath);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawTreePanel(rootPath);
            DrawPreviewPanel(rootPath);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader(string rootPath)
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("本地存档 (POCO) 物理浏览器", EditorStyles.largeLabel);

            GUILayout.FlexibleSpace();
            _previewMode = (PreviewMode)EditorGUILayout.EnumPopup(_previewMode, GUILayout.Width(80));

            if (GUILayout.Button("打开目录", GUILayout.Width(80)))
            {
                EnsureFolder(rootPath);
                EditorUtility.RevealInFinder(rootPath);
            }

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.2f));
            GUILayout.Space(5);
        }

        private void DrawTreePanel(string rootPath)
        {
            EditorGUILayout.BeginVertical("helpbox", GUILayout.Width(position.width * 0.4f));
            EditorGUILayout.LabelField("目录树 (Save Tree)", EditorStyles.boldLabel);
            DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.2f));

            _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll);
            DrawDirectoryNode(rootPath, 0, true);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawDirectoryNode(string directoryPath, int indent, bool isRoot)
        {
            if (!Directory.Exists(directoryPath)) return;

            string displayName = isRoot ? "Root (Saves)" : Path.GetFileName(directoryPath);
            bool expanded = _expandedDirectories.Contains(directoryPath);

            EditorGUI.indentLevel = indent;

            // 使用内置的文件夹图标
            GUIContent folderIcon = EditorGUIUtility.IconContent("Folder Icon");
            folderIcon.text = $" {displayName}";

            bool nextExpanded = EditorGUILayout.Foldout(expanded, folderIcon, true);
            if (nextExpanded) _expandedDirectories.Add(directoryPath);
            else _expandedDirectories.Remove(directoryPath);

            if (!nextExpanded) return;

            // 递归子目录
            string[] subDirectories = Directory.GetDirectories(directoryPath);
            Array.Sort(subDirectories, StringComparer.OrdinalIgnoreCase);
            foreach (string subDir in subDirectories)
            {
                DrawDirectoryNode(subDir, indent + 1, false);
            }

            // 渲染文件
            string[] files = Directory.GetFiles(directoryPath);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string filePath in files)
            {
                DrawFileNode(filePath, indent + 1);
            }
        }

        private void DrawFileNode(string filePath, int indent)
        {
            string fileName = Path.GetFileName(filePath);
            bool isSelected = string.Equals(_selectedFilePath, filePath, StringComparison.OrdinalIgnoreCase);

            EditorGUI.indentLevel = indent;
            EditorGUILayout.BeginHorizontal();

            GUIContent fileIcon = EditorGUIUtility.IconContent("TextAsset Icon");
            fileIcon.text = $" {fileName}";

            GUIStyle labelStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
            
            var defaultColor = GUI.contentColor;
            if (isSelected) GUI.contentColor = new Color(0.4f, 0.8f, 1f);

            if (GUILayout.Button(fileIcon, labelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                SelectFile(filePath);
            }
            GUI.contentColor = defaultColor;

            // 小巧的删除按钮
            if (GUILayout.Button("X", GUILayout.Width(22f), GUILayout.Height(18f)))
            {
                TryDeleteFile(filePath);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewPanel(string rootPath)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("文件预览 (Preview)", EditorStyles.boldLabel);
            DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.2f));

            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                EditorGUILayout.HelpBox("请在左侧选择一个存档文件以查看其内部数据。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("绝对路径:", _selectedFilePath, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("文件大小:", GetSelectedSizeText());
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("定位文件", GUILayout.Width(100f)))
            {
                EditorUtility.RevealInFinder(_selectedFilePath);
            }

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("删除此存档", GUILayout.Width(100f)))
            {
                TryDeleteFile(_selectedFilePath);
            }
            GUI.backgroundColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, "box");
            string content = BuildPreviewContent();
            // 用 TextArea 展示，允许选择复制
            EditorGUILayout.TextArea(content, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        #region 数据加载与预览转换

        private void SelectFile(string filePath)
        {
            _selectedFilePath = filePath;
            if (!File.Exists(filePath))
            {
                _selectedBytes = null;
                _selectedBytesTruncated = false;
                return;
            }

            byte[] allBytes = File.ReadAllBytes(filePath);
            if (allBytes.Length > PreviewMaxBytes)
            {
                _selectedBytes = new byte[PreviewMaxBytes];
                Buffer.BlockCopy(allBytes, 0, _selectedBytes, 0, PreviewMaxBytes);
                _selectedBytesTruncated = true;
            }
            else
            {
                _selectedBytes = allBytes;
                _selectedBytesTruncated = false;
            }
        }

        private string BuildPreviewContent()
        {
            if (_selectedBytes == null) return "(文件不存在或读取失败)";
            if (_selectedBytes.Length == 0) return "(空文件)";

            switch (_previewMode)
            {
                case PreviewMode.Utf8: return BuildUtf8Preview();
                case PreviewMode.Hex: return BuildHexPreview();
                default: return IsProbablyText(_selectedBytes) ? BuildUtf8Preview() : BuildHexPreview();
            }
        }

        private string BuildUtf8Preview()
        {
            string text = Encoding.UTF8.GetString(_selectedBytes);
            return _selectedBytesTruncated ? text + "\n\n... (预览已被截断，文件过大)" : text;
        }

        private string BuildHexPreview()
        {
            StringBuilder sb = new StringBuilder(_selectedBytes.Length * 4);
            const int lineWidth = 16;

            for (int i = 0; i < _selectedBytes.Length; i += lineWidth)
            {
                sb.Append(i.ToString("X8")).Append(" | ");
                int end = Mathf.Min(i + lineWidth, _selectedBytes.Length);
                for (int j = i; j < end; j++)
                {
                    sb.Append(_selectedBytes[j].ToString("X2")).Append(' ');
                }
                sb.AppendLine();
            }

            if (_selectedBytesTruncated) sb.AppendLine("\n... (预览已被截断)");
            return sb.ToString();
        }

        private static bool IsProbablyText(byte[] bytes)
        {
            int sampleLength = Mathf.Min(bytes.Length, 512);
            int badCount = 0;
            for (int i = 0; i < sampleLength; i++)
            {
                byte b = bytes[i];
                bool normal = b == 9 || b == 10 || b == 13 || (b >= 32 && b <= 126) || b >= 160;
                if (!normal) badCount++;
            }
            return badCount <= sampleLength * 0.1f;
        }

        #endregion

        #region 文件操作辅助

        private void TryDeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            if (EditorUtility.DisplayDialog("删除存档", $"确定要永久删除此存档文件吗？\n{Path.GetFileName(filePath)}", "删除", "取消"))
            {
                File.Delete(filePath);
                if (string.Equals(_selectedFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedFilePath = null;
                    _selectedBytes = null;
                }
                Repaint();
            }
        }

        private void EnsureFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private string GetSelectedSizeText()
        {
            if (_selectedFilePath == null || !File.Exists(_selectedFilePath)) return "-";
            long size = new FileInfo(_selectedFilePath).Length;
            return _selectedBytesTruncated ? $"> {PreviewMaxBytes} bytes (已截断预览)" : $"{size} bytes";
        }

        private void DrawLine(Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color);
        }

        #endregion
    }
}