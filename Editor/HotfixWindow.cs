using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using HybridCLR.Editor.Commands;

namespace GoveKits.Editor
{
    public class HotfixWindow : EditorWindow
    {
        private string _outputDir = "Assets/GameRes/HybridCLRBytes";
        private Vector2 _scrollPos;

        [MenuItem("GoveKits/Hotfix", false, 203)]
        public static void ShowWindow()
        {
            var window = GetWindow<HotfixWindow>("HybridCLR Build Tool");
            window.minSize = new Vector2(450, 550);
            window.Show();
        }

        private void OnEnable()
        {
            _outputDir = EditorPrefs.GetString("GoveKits_HybridCLR_OutputDir", "Assets/GameRes/HybridCLRBytes");
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("GoveKits_HybridCLR_OutputDir", _outputDir);
        }

        private void OnGUI()
        {
            DrawHeader();
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawConfigSection();
            DrawHotfixSection();
            DrawAOTSection();
            EditorGUILayout.EndScrollView();

            DrawActionButtons();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("HybridCLR 到 YooAsset 资源流转工具", EditorStyles.largeLabel);
            if (GUILayout.Button("打开设置", GUILayout.Width(80)))
            {
                Selection.activeObject = HybridCLRSettings.Instance;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("基础配置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");
            
            EditorGUIUtility.labelWidth = 120;
            _outputDir = EditorGUILayout.TextField("YooAsset 收集目录", _outputDir);
            EditorGUILayout.LabelField("当前构建平台", EditorUserBuildSettings.activeBuildTarget.ToString());
            EditorGUIUtility.labelWidth = 0; 
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        #region 数据获取核心逻辑

        // 1. 只获取 Asmdef 强引用的热更名称
        private List<string> GetHotUpdateAsmdefNames()
        {
            List<string> result = new List<string>();
            var defs = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions;
            if (defs != null)
            {
                foreach (var asmdef in defs)
                {
                    if (asmdef != null && !result.Contains(asmdef.name))
                    {
                        result.Add(asmdef.name);
                    }
                }
            }
            return result;
        }

        // 2. 智能解析生成的 AOTGenericReferences.cs 文件
        private List<string> ParseAOTGenericReferences()
        {
            List<string> result = new List<string>();
            string filePath = Path.Combine(Application.dataPath, HybridCLRSettings.Instance.outputAOTGenericReferenceFile);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return result; // 文件不存在直接返回空
            }

            string[] lines = File.ReadAllLines(filePath);
            bool inList = false;

            foreach (string line in lines)
            {

                // 定位到数组声明处
                if (line.Contains("PatchedAOTAssemblyList")) 
                { 
                    inList = true; 
                    continue; 
                }

                if (inList)
                {
                    // 遇到大括号结束
                    if (line.Contains("}")) break; 

                    // 提取 "xxx.dll"
                    int start = line.IndexOf('"');
                    int end = line.LastIndexOf('"');
                    if (start >= 0 && end > start)
                    {
                        string dllName = line.Substring(start + 1, end - start - 1);
                        if (dllName.EndsWith(".dll"))
                        {
                            result.Add(dllName);
                        }
                    }
                }
            }
            return result;
        }

        #endregion

        private void DrawHotfixSection()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string hotfixDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);

            EditorGUILayout.LabelField("🔥 热更程序集 (基于 Asmdef 定义)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"源目录: {hotfixDir}", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginVertical("helpbox");
            
            var hotfixNames = GetHotUpdateAsmdefNames();
            
            if (hotfixNames.Count == 0)
            {
                EditorGUILayout.HelpBox("未配置热更程序集！\n请点击右上角「打开设置」，将你的热更 .asmdef 文件拖入 Hot Update Assembly Definitions 列表中。", MessageType.Error);
            }
            else
            {
                DrawTableHeader();
                foreach (var dllName in hotfixNames)
                {
                    string fileName = dllName + ".dll";
                    string sourcePath = Path.Combine(hotfixDir, fileName);
                    DrawStatusRow(fileName, sourcePath);
                }
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawAOTSection()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string aotFilePath = Path.Combine(Application.dataPath, HybridCLRSettings.Instance.outputAOTGenericReferenceFile);

            EditorGUILayout.LabelField("🛡️ AOT 元数据程序集 (基于自动解析)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"解析文件: {aotFilePath}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"源目录: {aotDir}", EditorStyles.miniLabel);

            EditorGUILayout.BeginVertical("helpbox");
            
            if (!File.Exists(aotFilePath))
            {
                EditorGUILayout.HelpBox($"找不到 AOT 引用清单文件！\n请前往菜单栏点击 HybridCLR -> Generate -> All 进行生成。", MessageType.Warning);
            }
            else
            {
                var aotAssemblies = ParseAOTGenericReferences();

                if (aotAssemblies.Count == 0)
                {
                    EditorGUILayout.HelpBox("AOT 清单文件为空，当前热更代码未依赖任何 AOT 泛型。", MessageType.Info);
                }
                else
                {
                    DrawTableHeader();
                    foreach (var fileName in aotAssemblies)
                    {
                        string sourcePath = Path.Combine(aotDir, fileName);
                        DrawStatusRow(fileName, sourcePath);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawActionButtons()
        {
            DrawLine();
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. 执行 CompileDll", GUILayout.Height(30)))
            {
                CompileDllCommand.CompileDllActiveBuildTarget();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("2. 同步至 YooAsset 目录", GUILayout.Height(30)))
            {
                PerformCopy();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        #region UI 辅助绘制方法

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("程序集名称", EditorStyles.boldLabel, GUILayout.Width(250));
            GUILayout.Label("编译状态", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        }

        private void DrawStatusRow(string fileName, string sourcePath)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(fileName, GUILayout.Width(250));
            
            bool exists = File.Exists(sourcePath);
            GUIContent statusIcon = exists ? EditorGUIUtility.IconContent("TestPassed") : EditorGUIUtility.IconContent("TestFailed");
            string statusText = exists ? " Ready" : " Missing";
            
            var defaultColor = GUI.contentColor;
            GUI.contentColor = exists ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            GUILayout.Label(new GUIContent(statusText, statusIcon.image));
            GUI.contentColor = defaultColor;
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLine(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        #endregion

        #region 核心逻辑

        private void PerformCopy()
        {
            if (string.IsNullOrEmpty(_outputDir))
            {
                EditorUtility.DisplayDialog("参数错误", "YooAsset 收集目录不能为空。", "确定");
                return;
            }

            if (Directory.Exists(_outputDir))
            {
                Directory.Delete(_outputDir, true);
            }
            Directory.CreateDirectory(_outputDir);

            int copyCount = 0;
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string hotfixDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            string aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

            // 1. 拷贝基于 Asmdef 的热更程序集
            var hotfixNames = GetHotUpdateAsmdefNames();
            foreach (var dllName in hotfixNames)
            {
                string fileName = dllName + ".dll";
                string sourcePath = Path.Combine(hotfixDir, fileName);
                string destPath = Path.Combine(_outputDir, fileName + ".bytes");

                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, true);
                    copyCount++;
                }
            }

            // 2. 拷贝基于代码解析的 AOT 程序集
            var aotAssemblies = ParseAOTGenericReferences();
            foreach (var fileName in aotAssemblies)
            {
                string sourcePath = Path.Combine(aotDir, fileName);
                string destPath = Path.Combine(_outputDir, fileName + ".bytes");

                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, true);
                    copyCount++;
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("同步完成", $"已成功提取 {copyCount} 个 .bytes 资源至指定目录。\n可前往 YooAsset 面板进行构建。", "确定");
        }

        #endregion
    }
}