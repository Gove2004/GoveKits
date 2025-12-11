using System.IO;
using ExcelDataReader;
using GoveKits.Config;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class ExcelConfigEditor : EditorWindow
    {
        // 配置键名
        private const string KEY_EXCEL_PATH = "GoveKits_ExcelPath";
        private const string KEY_CODE_PATH = "GoveKits_CodePath";
        private const string KEY_JSON_PATH = "GoveKits_JsonPath";
        private const string KEY_NAMESPACE = "GoveKits_Namespace";

        private string _excelFolderPath;
        private string _codeOutputFolder;
        private string _jsonOutputFolder;
        private string _namespaceName;

        [MenuItem("GoveKits/Excel2Json")]
        public static void ShowWindow()
        {
            var win = GetWindow<ExcelConfigEditor>("Excel2Json");
            win.minSize = new Vector2(400, 300);
            win.LoadPrefs();
        }

        private void OnEnable() => LoadPrefs();

        private void OnGUI()
        {
            GUILayout.Label("Excel 导表工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 绘制路径配置框
            DrawPathSetting("Excel 目录", ref _excelFolderPath, KEY_EXCEL_PATH);
            DrawPathSetting("DTO 代码目录", ref _codeOutputFolder, KEY_CODE_PATH);
            DrawPathSetting("JSON 数据目录", ref _jsonOutputFolder, KEY_JSON_PATH);
            
            _namespaceName = EditorGUILayout.TextField("命名空间", _namespaceName);
            if (GUI.changed) EditorPrefs.SetString(KEY_NAMESPACE, _namespaceName);

            EditorGUILayout.Space(20);
            GUILayout.Label("操作流程", EditorStyles.boldLabel);

            // 1. 清空
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("1. 清空旧文件 (Clean)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要清空生成目录下的所有文件吗？", "确定", "取消"))
                {
                    ClearFolders();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // 2. 分步生成
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("2. 仅生成 C# 代码", GUILayout.Height(30))) RunProcess(true, false);
            if (GUILayout.Button("3. 仅生成 JSON 数据", GUILayout.Height(30))) RunProcess(false, true);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 3. 一键生成
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("一键全部生成 (Generate All)", GUILayout.Height(40))) RunProcess(true, true);
            GUI.backgroundColor = Color.white;
        }

        private void DrawPathSetting(string label, ref string path, string key)
        {
            GUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                string temp = EditorUtility.OpenFolderPanel("选择目录", Application.dataPath, "");
                if (!string.IsNullOrEmpty(temp))
                {
                    // 转换为相对路径
                    if (temp.StartsWith(Application.dataPath))
                        temp = "Assets" + temp.Substring(Application.dataPath.Length);
                    
                    path = temp;
                    EditorPrefs.SetString(key, path);
                }
            }
            GUILayout.EndHorizontal();
            // 实时保存输入更改
            if (GUI.changed) EditorPrefs.SetString(key, path);
        }

        private void LoadPrefs()
        {
            _excelFolderPath = EditorPrefs.GetString(KEY_EXCEL_PATH, "Assets/Config/Excel");
            _codeOutputFolder = EditorPrefs.GetString(KEY_CODE_PATH, "Assets/Config/DTO");
            _jsonOutputFolder = EditorPrefs.GetString(KEY_JSON_PATH, "Assets/Resources/Config/Json");
            _namespaceName = EditorPrefs.GetString(KEY_NAMESPACE, "GoveKits.Config");
        }

        private void ClearFolders()
        {
            CleanDir(_codeOutputFolder, "*.cs");
            CleanDir(_jsonOutputFolder, "*.json");
            AssetDatabase.Refresh();
            DebugLogger.Log("ExcelConfigEditor", "[Clean] 清理完成");
        }

        private void CleanDir(string path, string pattern)
        {
            if (!Directory.Exists(path)) return;
            foreach (var file in Directory.GetFiles(path, pattern)) File.Delete(file);
        }

        private void RunProcess(bool genCode, bool genJson)
        {
            if (!Directory.Exists(_excelFolderPath))
            {
                DebugLogger.LogError("ExcelConfigEditor", $"Excel目录不存在: {_excelFolderPath}");
                return;
            }

            if (genCode && !Directory.Exists(_codeOutputFolder)) Directory.CreateDirectory(_codeOutputFolder);
            if (genJson && !Directory.Exists(_jsonOutputFolder)) Directory.CreateDirectory(_jsonOutputFolder);

            int count = 0;
            string[] files = Directory.GetFiles(_excelFolderPath, "*.xlsx");

            try
            {
                foreach (var file in files)
                {
                    if (Path.GetFileName(file).StartsWith("~$")) continue;

                    // 核心调用：使用 ExcelReader 读取文件，然后分别传给生成器
                    using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                        {
                            var dataSet = reader.AsDataSet();
                            string excelName = Path.GetFileNameWithoutExtension(file);

                            // 如果需要生成代码
                            if (genCode)
                                ExcelToCode.Generate(dataSet, excelName, _codeOutputFolder, _namespaceName);

                            // 如果需要生成 Json
                            if (genJson)
                                ExcelToJson.Generate(dataSet, excelName, _jsonOutputFolder);
                            
                            count++;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.LogError("ExcelConfigEditor", $"生成失败: {ex}");
                EditorUtility.ClearProgressBar();
                return;
            }

            AssetDatabase.Refresh();
            string msg = $"处理完成！({count} 个Excel文件)\n";
            if (genCode) msg += "- 代码已更新 (需等待编译)\n";
            if (genJson) msg += "- 数据已更新";
            EditorUtility.DisplayDialog("完成", msg, "OK");
        }
    }
}