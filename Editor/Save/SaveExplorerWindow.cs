// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using UnityEditor;
// using UnityEngine;

// namespace GoveKits.Editor.Save
// {
//     /// <summary>
//     /// Save 目录可视化工具窗口。
//     /// </summary>
//     public class SaveExplorerWindow : EditorWindow
//     {
//         private enum PreviewMode
//         {
//             Auto,
//             Utf8,
//             Hex,
//         }

//         private const string SaveRootFolderName = "Saves";
//         private const int PreviewMaxBytes = 256 * 1024;

//         private readonly HashSet<string> expandedDirectories = new();

//         private Vector2 treeScroll;
//         private Vector2 previewScroll;

//         private string selectedFilePath;
//         private byte[] selectedBytes;
//         private bool selectedBytesTruncated;

//         private PreviewMode previewMode = PreviewMode.Auto;

//         private string SaveRootPath => Path.Combine(Application.persistentDataPath, SaveRootFolderName);

//         [MenuItem("GoveKits/Storage/Save Explorer")]
//         public static void ShowWindow()
//         {
//             GetWindow<SaveExplorerWindow>("Save Explorer");
//         }

//         private void OnEnable()
//         {
//             EnsureSaveFolder();
//             expandedDirectories.Add(SaveRootPath);
//         }

//         private void OnGUI()
//         {
//             DrawToolbar();

//             if (!Directory.Exists(SaveRootPath))
//             {
//                 EditorGUILayout.HelpBox("Save 目录不存在。", MessageType.Info);
//                 if (GUILayout.Button("Create Save Folder"))
//                 {
//                     EnsureSaveFolder();
//                 }

//                 return;
//             }

//             EditorGUILayout.BeginHorizontal();
//             DrawTreePanel();
//             DrawPreviewPanel();
//             EditorGUILayout.EndHorizontal();
//         }

//         private void DrawToolbar()
//         {
//             EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//             GUILayout.Label("Save Explorer", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();

//             previewMode = (PreviewMode)EditorGUILayout.EnumPopup(previewMode, EditorStyles.toolbarPopup, GUILayout.Width(90));

//             if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
//             {
//                 Repaint();
//             }

//             if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton))
//             {
//                 EnsureSaveFolder();
//                 EditorUtility.RevealInFinder(SaveRootPath);
//             }

//             Color oldColor = GUI.color;
//             GUI.color = new Color(1f, 0.45f, 0.45f);
//             if (GUILayout.Button("Delete All", EditorStyles.toolbarButton))
//             {
//                 TryDeleteAll();
//             }

//             GUI.color = oldColor;
//             EditorGUILayout.EndHorizontal();
//         }

//         private void DrawTreePanel()
//         {
//             EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.42f));
//             GUILayout.Label("Save Tree", EditorStyles.boldLabel);

//             treeScroll = EditorGUILayout.BeginScrollView(treeScroll);
//             DrawDirectoryNode(SaveRootPath, 0, true);
//             EditorGUILayout.EndScrollView();

//             EditorGUILayout.EndVertical();
//         }

//         private void DrawDirectoryNode(string directoryPath, int indent, bool isRoot)
//         {
//             if (!Directory.Exists(directoryPath))
//             {
//                 return;
//             }

//             string displayName = isRoot ? SaveRootFolderName : Path.GetFileName(directoryPath);
//             bool expanded = expandedDirectories.Contains(directoryPath);

//             EditorGUI.indentLevel = indent;
//             bool nextExpanded = EditorGUILayout.Foldout(expanded, displayName, true);
//             if (nextExpanded)
//             {
//                 expandedDirectories.Add(directoryPath);
//             }
//             else
//             {
//                 expandedDirectories.Remove(directoryPath);
//             }

//             if (!nextExpanded)
//             {
//                 return;
//             }

//             string[] subDirectories = Directory.GetDirectories(directoryPath);
//             Array.Sort(subDirectories, StringComparer.OrdinalIgnoreCase);
//             foreach (string subDirectory in subDirectories)
//             {
//                 DrawDirectoryNode(subDirectory, indent + 1, false);
//             }

//             string[] files = Directory.GetFiles(directoryPath);
//             Array.Sort(files, StringComparer.OrdinalIgnoreCase);
//             foreach (string filePath in files)
//             {
//                 DrawFileNode(filePath, indent + 1);
//             }
//         }

//         private void DrawFileNode(string filePath, int indent)
//         {
//             string fileName = Path.GetFileName(filePath);
//             bool isSelected = string.Equals(selectedFilePath, filePath, StringComparison.OrdinalIgnoreCase);

//             EditorGUI.indentLevel = indent;
//             EditorGUILayout.BeginHorizontal();

//             GUIStyle labelStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
//             if (GUILayout.Button(fileName, labelStyle, GUILayout.ExpandWidth(true)))
//             {
//                 SelectFile(filePath);
//             }

//             if (GUILayout.Button("X", GUILayout.Width(22f)))
//             {
//                 TryDeleteFile(filePath);
//             }

//             EditorGUILayout.EndHorizontal();
//         }

//         private void DrawPreviewPanel()
//         {
//             EditorGUILayout.BeginVertical();
//             GUILayout.Label("Preview", EditorStyles.boldLabel);

//             if (string.IsNullOrEmpty(selectedFilePath))
//             {
//                 EditorGUILayout.HelpBox("选择一个存档文件以查看内容。", MessageType.Info);
//                 EditorGUILayout.EndVertical();
//                 return;
//             }

//             EditorGUILayout.LabelField("File", ToRelativePath(selectedFilePath));
//             EditorGUILayout.LabelField("Size", GetSelectedSizeText());

//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("Open File Location", GUILayout.Width(140f)))
//             {
//                 EditorUtility.RevealInFinder(selectedFilePath);
//             }

//             Color oldColor = GUI.color;
//             GUI.color = new Color(1f, 0.45f, 0.45f);
//             if (GUILayout.Button("Delete Selected", GUILayout.Width(120f)))
//             {
//                 TryDeleteFile(selectedFilePath);
//             }

//             GUI.color = oldColor;
//             EditorGUILayout.EndHorizontal();

//             previewScroll = EditorGUILayout.BeginScrollView(previewScroll);
//             string content = BuildPreviewContent();
//             EditorGUILayout.TextArea(content, GUILayout.ExpandHeight(true));
//             EditorGUILayout.EndScrollView();
//             EditorGUILayout.EndVertical();
//         }

//         private string BuildPreviewContent()
//         {
//             if (selectedBytes == null)
//             {
//                 return "(文件不存在或读取失败)";
//             }

//             if (selectedBytes.Length == 0)
//             {
//                 return "(空文件)";
//             }

//             switch (previewMode)
//             {
//                 case PreviewMode.Utf8:
//                     return BuildUtf8Preview();
//                 case PreviewMode.Hex:
//                     return BuildHexPreview();
//                 default:
//                     return IsProbablyText(selectedBytes) ? BuildUtf8Preview() : BuildHexPreview();
//             }
//         }

//         private string BuildUtf8Preview()
//         {
//             string text = Encoding.UTF8.GetString(selectedBytes);
//             if (selectedBytesTruncated)
//             {
//                 return text + "\n\n... (preview truncated)";
//             }

//             return text;
//         }

//         private string BuildHexPreview()
//         {
//             StringBuilder sb = new StringBuilder(selectedBytes.Length * 4);
//             const int lineWidth = 16;

//             for (int i = 0; i < selectedBytes.Length; i += lineWidth)
//             {
//                 sb.Append(i.ToString("X8")).Append(": ");
//                 int end = Mathf.Min(i + lineWidth, selectedBytes.Length);
//                 for (int j = i; j < end; j++)
//                 {
//                     sb.Append(selectedBytes[j].ToString("X2")).Append(' ');
//                 }

//                 sb.AppendLine();
//             }

//             if (selectedBytesTruncated)
//             {
//                 sb.AppendLine("...");
//                 sb.AppendLine("(preview truncated)");
//             }

//             return sb.ToString();
//         }

//         private static bool IsProbablyText(byte[] bytes)
//         {
//             int sampleLength = Mathf.Min(bytes.Length, 512);
//             int badCount = 0;
//             for (int i = 0; i < sampleLength; i++)
//             {
//                 byte b = bytes[i];
//                 bool normal = b == 9 || b == 10 || b == 13 || (b >= 32 && b <= 126) || b >= 160;
//                 if (!normal)
//                 {
//                     badCount++;
//                 }
//             }

//             return badCount <= sampleLength * 0.1f;
//         }

//         private void SelectFile(string filePath)
//         {
//             selectedFilePath = filePath;

//             if (!File.Exists(filePath))
//             {
//                 selectedBytes = null;
//                 selectedBytesTruncated = false;
//                 return;
//             }

//             byte[] allBytes = File.ReadAllBytes(filePath);
//             if (allBytes.Length > PreviewMaxBytes)
//             {
//                 selectedBytes = new byte[PreviewMaxBytes];
//                 Buffer.BlockCopy(allBytes, 0, selectedBytes, 0, PreviewMaxBytes);
//                 selectedBytesTruncated = true;
//             }
//             else
//             {
//                 selectedBytes = allBytes;
//                 selectedBytesTruncated = false;
//             }
//         }

//         private void TryDeleteFile(string filePath)
//         {
//             if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
//             {
//                 return;
//             }

//             bool confirmed = EditorUtility.DisplayDialog(
//                 "Delete Save File",
//                 $"Delete save file?\n{ToRelativePath(filePath)}",
//                 "Delete",
//                 "Cancel");

//             if (!confirmed)
//             {
//                 return;
//             }

//             File.Delete(filePath);
//             if (string.Equals(selectedFilePath, filePath, StringComparison.OrdinalIgnoreCase))
//             {
//                 selectedFilePath = null;
//                 selectedBytes = null;
//                 selectedBytesTruncated = false;
//             }

//             Repaint();
//         }

//         private void TryDeleteAll()
//         {
//             bool confirmed = EditorUtility.DisplayDialog(
//                 "Delete All Saves",
//                 "Delete ALL save files? This cannot be undone.",
//                 "Delete All",
//                 "Cancel");

//             if (!confirmed)
//             {
//                 return;
//             }

//             if (Directory.Exists(SaveRootPath))
//             {
//                 Directory.Delete(SaveRootPath, true);
//             }

//             Directory.CreateDirectory(SaveRootPath);
//             selectedFilePath = null;
//             selectedBytes = null;
//             selectedBytesTruncated = false;
//             expandedDirectories.Clear();
//             expandedDirectories.Add(SaveRootPath);
//             Repaint();
//         }

//         private void EnsureSaveFolder()
//         {
//             if (!Directory.Exists(SaveRootPath))
//             {
//                 Directory.CreateDirectory(SaveRootPath);
//             }
//         }

//         private string ToRelativePath(string filePath)
//         {
//             if (string.IsNullOrEmpty(filePath))
//             {
//                 return string.Empty;
//             }

//             string root = SaveRootPath.Replace('\\', '/').TrimEnd('/');
//             string full = filePath.Replace('\\', '/');
//             if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
//             {
//                 return full.Substring(root.Length).TrimStart('/');
//             }

//             return filePath;
//         }

//         private string GetSelectedSizeText()
//         {
//             if (selectedFilePath == null || !File.Exists(selectedFilePath))
//             {
//                 return "-";
//             }

//             long size = new FileInfo(selectedFilePath).Length;
//             return selectedBytesTruncated ? $"> {PreviewMaxBytes} bytes (previewed)" : $"{size} bytes";
//         }
//     }
// }
