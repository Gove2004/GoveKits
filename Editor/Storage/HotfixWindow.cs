#if HYBRIDCLR
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using HybridCLR.Editor.Commands;

namespace GoveKits.Editor
{
    /// <summary>
    /// HybridCLR 到 YooAsset 资源流转工具窗口。
    /// 展示热更程序集和 AOT 元数据程序集的编译状态，支持一键编译和同步至 YooAsset 目录。
    /// </summary>
    public class HotfixWindow : GoveKitsEditorWindow
    {
        private string _outputDir = "Assets/GameRes/HybridCLRBytes";

        [MenuItem("GoveKits/Hotfix", false, 203)]
        public static void ShowWindow()
        {
            var window = GetWindow<HotfixWindow>("HybridCLR Build Tool");
            window.minSize = new Vector2(450, 550);
            window.Show();
        }

        protected override void OnGoveWindowEnable()
        {
            _outputDir = EditorPrefs.GetString("GoveKits_HybridCLR_OutputDir", "Assets/GameRes/HybridCLRBytes");
        }

        protected override void OnGoveWindowDisable()
        {
            EditorPrefs.SetString("GoveKits_HybridCLR_OutputDir", _outputDir);
        }

        protected override void OnGoveDrawContent()
        {
            DrawConfigSection();
            DrawHotfixSection();
            DrawAOTSection();
        }

        protected override void DrawHeader()
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

        protected override void DrawSearchToolbar()
        {
            DrawActionButtons();
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

        private List<string> GetHotUpdateAsmdefNames()
        {
            List<string> result = new List<string>();
            var defs = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions;
            if (defs != null)
            {
                foreach (var asmdef in defs)
                {
                    if (asmdef != null && !result.Contains(asmdef.name))
                        result.Add(asmdef.name);
                }
            }
            return result;
        }

        private List<string> ParseAOTGenericReferences()
        {
            List<string> result = new List<string>();
            string filePath = Path.Combine(Application.dataPath, HybridCLRSettings.Instance.outputAOTGenericReferenceFile);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return result;

            string[] lines = File.ReadAllLines(filePath);
            bool inList = false;

            foreach (string line in lines)
            {
                if (line.Contains("PatchedAOTAssemblyList")) { inList = true; continue; }
                if (inList)
                {
                    if (line.Contains("}")) break;
                    int start = line.IndexOf('"');
                    int end = line.LastIndexOf('"');
                    if (start >= 0 && end > start)
                    {
                        string dllName = line.Substring(start + 1, end - start - 1);
                        if (dllName.EndsWith(".dll"))
                            result.Add(dllName);
                    }
                }
            }
            return result;
        }

        private void DrawHotfixSection()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string hotfixDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);

            EditorGUILayout.LabelField("热更程序集 (基于 Asmdef 定义)", EditorStyles.boldLabel);
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

            EditorGUILayout.LabelField("AOT 元数据程序集 (基于自动解析)", EditorStyles.boldLabel);
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
            string statusText = exists ? "就绪" : "缺失";

            var defaultColor = GUI.contentColor;
            GUI.contentColor = exists ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            GUILayout.Label(new GUIContent(statusText, statusIcon.image));
            GUI.contentColor = defaultColor;

            EditorGUILayout.EndHorizontal();
        }

        private void PerformCopy()
        {
            if (string.IsNullOrEmpty(_outputDir))
            {
                EditorUtility.DisplayDialog("参数错误", "YooAsset 收集目录不能为空。", "确定");
                return;
            }

            string assetsPath = Path.Combine(Application.dataPath, "..");
            string absoluteOutput = Path.GetFullPath(_outputDir);

            // 黑名单：删除目标为盘符根/用户目录/项目根本身时直接拒绝（原逻辑只对项目内路径确认，
            // 项目外路径反而无确认直接递归删除，保护方向反了）
            string projectRoot = Path.GetFullPath(assetsPath);
            string userProfile = Path.GetFullPath(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));

            bool IsProtectedRoot(string dir)
            {
                if (string.IsNullOrEmpty(dir)) return false;
                string full = Path.GetFullPath(dir)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return absoluteOutput.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Equals(full, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (IsProtectedRoot(drive.RootDirectory.FullName))
                {
                    EditorUtility.DisplayDialog("已拒绝",
                        $"目标路径是盘符根目录：{_outputDir}\n为防止误删，操作已取消。", "确定");
                    return;
                }
            }

            if (IsProtectedRoot(userProfile) || IsProtectedRoot(projectRoot))
            {
                EditorUtility.DisplayDialog("已拒绝",
                    $"目标路径指向受保护目录（用户目录/项目根）：{_outputDir}\n为防止误删，操作已取消。", "确定");
                return;
            }

            // 无条件确认：清空目标目录不可恢复（项目内外同等对待）
            if (!EditorUtility.DisplayDialog("警告",
                $"将清空并重建目标目录：{_outputDir}\n删除后不可恢复，确定继续？", "确定", "取消"))
            {
                return;
            }

            try
            {
                if (Directory.Exists(_outputDir))
                {
                    Directory.Delete(_outputDir, true);
                }
                Directory.CreateDirectory(_outputDir);

                int copyCount = 0;
                int failCount = 0;
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                string hotfixDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
                string aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

                var hotfixNames = GetHotUpdateAsmdefNames();
                foreach (var dllName in hotfixNames)
                {
                    string fileName = dllName + ".dll";
                    string sourcePath = Path.Combine(hotfixDir, fileName);
                    string destPath = Path.Combine(_outputDir, fileName + ".bytes");

                    if (File.Exists(sourcePath))
                    {
                        try { File.Copy(sourcePath, destPath, true); copyCount++; }
                        catch (Exception ex) { Debug.LogError($"[HotfixWindow] 拷贝失败 {fileName}: {ex.Message}"); failCount++; }
                    }
                }

                var aotAssemblies = ParseAOTGenericReferences();
                foreach (var fileName in aotAssemblies)
                {
                    string sourcePath = Path.Combine(aotDir, fileName);
                    string destPath = Path.Combine(_outputDir, fileName + ".bytes");

                    if (File.Exists(sourcePath))
                    {
                        try { File.Copy(sourcePath, destPath, true); copyCount++; }
                        catch (Exception ex) { Debug.LogError($"[HotfixWindow] 拷贝失败 {fileName}: {ex.Message}"); failCount++; }
                    }
                }

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("同步完成",
                    $"成功拷贝 {copyCount} 个文件{(failCount > 0 ? $"\n失败 {failCount} 个" : "")}。\n可前往 YooAsset 面板进行构建。", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("拷贝失败", $"发生错误:\n{ex.Message}", "确定");
            }
        }
    }
}
#endif
