using UnityEngine;
using UnityEditor;
using System.IO;
using GoveKits.Utility; // 引用你提供的CMD命名空间

namespace GoveKits.Editor
{
    public class ProtobufGeneratorWindow : EditorWindow
    {
        // 用于保存配置的Key
        private const string PREF_PROTOC_PATH = "GoveKits_ProtocPath";
        private const string PREF_PROTO_FILE_PATH = "GoveKits_ProtoFilePath";
        private const string PREF_OUTPUT_DIR = "GoveKits_ProtoOutputDir";

        private string _protocPath = "Assets/Plugins/protoc.exe";
        private string _protoFilePath = "";
        private string _outputDir = "";


        [MenuItem("GoveKits/Protobuf Generator")]
        public static void ShowWindow()
        {
            GetWindow<ProtobufGeneratorWindow>("Protobuf Generator");
        }

        private void OnEnable()
        {
            // 加载上次保存的配置
            _protocPath = EditorPrefs.GetString(PREF_PROTOC_PATH, "");
            _protoFilePath = EditorPrefs.GetString(PREF_PROTO_FILE_PATH, "");
            _outputDir = EditorPrefs.GetString(PREF_OUTPUT_DIR, "");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Protobuf 生成工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 1. 选择 protoc.exe 路径
            DrawPathSelection("Protoc 编译器路径 (protoc.exe)", ref _protocPath, "exe", false);

            GUILayout.Space(5);

            // 2. 选择 .proto 文件路径
            DrawPathSelection(".proto 源文件路径", ref _protoFilePath, "proto", false, () => 
            {
                // 如果输出目录为空，当选择proto文件时，自动默认输出到同级目录
                if (string.IsNullOrEmpty(_outputDir) && !string.IsNullOrEmpty(_protoFilePath))
                {
                    _outputDir = Path.GetDirectoryName(_protoFilePath);
                    SavePrefs();
                }
            });

            GUILayout.Space(5);

            // 3. 选择输出目录
            DrawPathSelection("C# 输出目录", ref _outputDir, "", true);

            GUILayout.Space(20);

            // 生成按钮
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("生成 C# 代码", GUILayout.Height(40)))
            {
                GenerateCode();
            }
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// 绘制路径选择的通用UI
        /// </summary>
        private void DrawPathSelection(string label, ref string pathVar, string extension, bool isFolder, System.Action onPathChanged = null)
        {
            GUILayout.Label(label);
            EditorGUILayout.BeginHorizontal();
            
            // 文本框允许手动修改
            string newPath = EditorGUILayout.TextField(pathVar);
            if (newPath != pathVar)
            {
                pathVar = newPath;
                SavePrefs();
                onPathChanged?.Invoke();
            }

            if (GUILayout.Button("浏览...", GUILayout.Width(80)))
            {
                string selectedPath = "";
                
                // 【修复】计算默认打开路径：如果当前路径为空或无效，则默认打开项目根目录
                string defaultOpenPath = "";
                if (!string.IsNullOrEmpty(pathVar))
                {
                    try 
                    {
                        // 只有当路径看起来合法时才尝试获取目录名
                        defaultOpenPath = isFolder ? pathVar : Path.GetDirectoryName(pathVar); 
                    }
                    catch 
                    { 
                        defaultOpenPath = ""; // 路径非法时回退到空
                    }
                }

                if (isFolder)
                {
                    selectedPath = EditorUtility.OpenFolderPanel("选择输出目录", defaultOpenPath, "");
                }
                else
                {
                    selectedPath = EditorUtility.OpenFilePanel("选择文件", defaultOpenPath, extension);
                }

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    pathVar = selectedPath;
                    SavePrefs(); // 立即保存
                    onPathChanged?.Invoke();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PREF_PROTOC_PATH, _protocPath);
            EditorPrefs.SetString(PREF_PROTO_FILE_PATH, _protoFilePath);
            EditorPrefs.SetString(PREF_OUTPUT_DIR, _outputDir);
        }

        private void GenerateCode()
        {
            // 1. 基础校验
            if (!File.Exists(_protocPath))
            {
                EditorUtility.DisplayDialog("错误", "找不到 protoc.exe，请检查路径。", "确定");
                return;
            }

            if (!File.Exists(_protoFilePath))
            {
                EditorUtility.DisplayDialog("错误", "找不到 .proto 文件，请检查路径。", "确定");
                return;
            }

            if (string.IsNullOrEmpty(_outputDir))
            {
                // 如果输出目录为空，默认设置为 proto 文件所在目录
                _outputDir = Path.GetDirectoryName(_protoFilePath);
            }

            if (!Directory.Exists(_outputDir))
            {
                try
                {
                    Directory.CreateDirectory(_outputDir);
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("错误", $"无法创建输出目录: {e.Message}", "确定");
                    return;
                }
            }

            // 2. 组装参数
            // 格式: --proto_path="[proto文件目录]" --csharp_out="[输出目录]" "[proto文件全路径]"
            string protoDir = Path.GetDirectoryName(_protoFilePath);
            
            // 使用双引号包裹路径以处理空格
            string args = $"--proto_path=\"{protoDir}\" --csharp_out=\"{_outputDir}\" \"{_protoFilePath}\"";

            // 3. 调用 CMD 工具
            // workingDir 设置为 proto 文件所在目录，有助于处理 proto 内部的 import 相对路径
            string[] result = CMD.ExecuteWithError(_protocPath, args, protoDir);

            string output = result[0];
            string error = result[1];
            string exitCodeStr = result[2];

            // 4. 处理结果
            if (exitCodeStr == "0" && string.IsNullOrEmpty(error))
            {
                // 成功
                LogManager.Log("Protoc", $"<color=green>[Protobuf] 生成成功!</color>\n输出路径: {_outputDir}");
                if(!string.IsNullOrEmpty(output)) LogManager.Log("Protoc", $"Protoc Output: {output}");
                
                // 刷新 Assets 目录，让 Unity 编译新生成的脚本
                AssetDatabase.Refresh();
            }
            else
            {
                // 失败
                string errorMsg = $"[Protobuf] 生成失败 (ExitCode: {exitCodeStr})\n错误信息:\n{error}";
                LogManager.LogError("Protoc", errorMsg);
                EditorUtility.DisplayDialog("生成失败", errorMsg, "确定");
            }
        }
    }
}