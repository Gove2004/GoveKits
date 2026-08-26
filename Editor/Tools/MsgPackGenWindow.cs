using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GoveKits.Editor
{
    /// <summary>
    /// MessagePack 批量代码生成器窗口。
    /// 支持管理多个 mpc 编译配置，批量或单独执行代码生成，异步调用 mpc 进程并捕获输出。
    /// </summary>
    public class MpcGenWindow : GoveKitsEditorWindow
    {
        private const string PrefsKey = "GoveKits_MpcGen_Configs";

        private List<MpcConfigItem> _configs = new List<MpcConfigItem>();
        private bool _isGenerating = false;

        [MenuItem("GoveKits/MsgPackGen", false, 301)]
        public static void ShowWindow()
        {
            var window = GetWindow<MpcGenWindow>("MPC 多包配置");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        protected override void OnGoveWindowEnable()
        {
            LoadConfigs();
        }

        protected override void OnGoveWindowDisable()
        {
            SaveConfigs();
        }

        protected override void OnGoveDrawContent()
        {
            DrawConfigs();
        }

        protected override void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MessagePack 批量代码生成器", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_isGenerating);
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button(_isGenerating ? "正在生成..." : "生成全部启用项", GUILayout.Width(130), GUILayout.Height(24)))
            {
                GenerateAllEnabled();
            }
            GUI.backgroundColor = defaultColor;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        protected override void DrawSearchToolbar()
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 新增配置", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                _configs.Add(new MpcConfigItem { Name = $"Config {_configs.Count + 1}" });
            }
            if (GUILayout.Button("展开全部", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                _configs.ForEach(c => c.IsExpanded = true);
            }
            if (GUILayout.Button("收起全部", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                _configs.ForEach(c => c.IsExpanded = false);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawConfigs()
        {
            if (_configs.Count == 0)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("暂无打包配置，请点击左上角【新增配置】。", MessageType.Info);
                return;
            }

            int indexToRemove = -1;

            for (int i = 0; i < _configs.Count; i++)
            {
                var cfg = _configs[i];
                EditorGUILayout.BeginVertical("helpbox");

                EditorGUILayout.BeginHorizontal();
                cfg.IsEnable = EditorGUILayout.Toggle(cfg.IsEnable, GUILayout.Width(20));
                cfg.IsExpanded = EditorGUILayout.Foldout(cfg.IsExpanded, cfg.Name, true, EditorStyles.foldoutHeader);

                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(_isGenerating);
                if (GUILayout.Button("单跑", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    GenerateSingleAsync(cfg);
                }
                EditorGUI.EndDisabledGroup();

                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    indexToRemove = i;
                }
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();

                if (cfg.IsExpanded)
                {
                    GUILayout.Space(5);
                    cfg.Name = EditorGUILayout.TextField("配置别名:", cfg.Name);
                    GUILayout.Space(5);

                    cfg.InputPath = DrawField("-i input path:", cfg.InputPath);
                    cfg.OutputPath = DrawField("-o output path:", cfg.OutputPath);

                    cfg.ClearOutputDirectory = EditorGUILayout.ToggleLeft("生成前清空输出目录", cfg.ClearOutputDirectory);
                    GUILayout.Space(5);

                    cfg.MapMode = EditorGUILayout.ToggleLeft("-m use map mode", cfg.MapMode);
                    cfg.Symbols = DrawField("-c conditional symbols:", cfg.Symbols);
                    cfg.ResolverName = DrawField("-r generated resolver name:", cfg.ResolverName);
                    cfg.Namespace = DrawField("-n namespace root name:", cfg.Namespace);
                    cfg.MultipleSymbols = DrawField("-ms generate #if-- files:", cfg.MultipleSymbols);
                    GUILayout.Space(5);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (indexToRemove >= 0)
            {
                _configs.RemoveAt(indexToRemove);
            }
        }

        private string DrawField(string label, string value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextField(value);
        }

        #region 数据存储

        private void SaveConfigs()
        {
            var wrapper = new MpcConfigListWrapper { Configs = _configs };
            string json = JsonUtility.ToJson(wrapper, true);
            EditorPrefs.SetString(PrefsKey, json);
        }

        private void LoadConfigs()
        {
            string json = EditorPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<MpcConfigListWrapper>(json);
                if (wrapper != null && wrapper.Configs != null)
                {
                    _configs = wrapper.Configs;
                    return;
                }
            }
            _configs = new List<MpcConfigItem> { new MpcConfigItem() };
        }

        #endregion

        #region 核心：异步生成

        private async void GenerateAllEnabled()
        {
            _isGenerating = true;
            int successCount = 0;

            try
            {
                foreach (var cfg in _configs)
                {
                    if (cfg.IsEnable)
                    {
                        bool ok = await ProcessSingleConfigAsync(cfg);
                        if (ok) successCount++;
                    }
                }

                EditorUtility.DisplayDialog("提示", $"批量打包完毕，成功 {successCount} 个！", "确定");
            }
            finally
            {
                _isGenerating = false;
                AssetDatabase.Refresh();
            }
        }

        private async void GenerateSingleAsync(MpcConfigItem cfg)
        {
            _isGenerating = true;
            try
            {
                await ProcessSingleConfigAsync(cfg);
            }
            finally
            {
                _isGenerating = false;
                AssetDatabase.Refresh();
            }
        }

        private async Task<bool> ProcessSingleConfigAsync(MpcConfigItem cfg)
        {
            if (cfg.ClearOutputDirectory && !string.IsNullOrWhiteSpace(cfg.OutputPath))
            {
                string fullOutputPath = Path.Combine(Application.dataPath, cfg.OutputPath);
                string normalizedOutputPath = Path.GetFullPath(fullOutputPath);
                string normalizedAssetsPath = Path.GetFullPath(Application.dataPath);

                if (normalizedOutputPath.StartsWith(normalizedAssetsPath) && normalizedOutputPath != normalizedAssetsPath)
                {
                    if (Directory.Exists(normalizedOutputPath))
                    {
                        try
                        {
                            Debug.Log($"<color=orange>选项已启用：正在清空目录 [{cfg.OutputPath}]...</color>");
                            Directory.Delete(normalizedOutputPath, true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"清空目录 {cfg.OutputPath} 失败: {e.Message}");
                            return false;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"出于安全考虑，跳过清空目录 [{cfg.OutputPath}]。该路径可能指向项目根目录或外部文件夹。");
                }
            }

            var sb = new StringBuilder();
            sb.Append($"-i \"{cfg.InputPath}\" -o \"{cfg.OutputPath}\"");

            if (cfg.MapMode) sb.Append(" -m");
            if (!string.IsNullOrWhiteSpace(cfg.Symbols)) sb.Append($" -c \"{cfg.Symbols}\"");
            if (!string.IsNullOrWhiteSpace(cfg.ResolverName)) sb.Append($" -r \"{cfg.ResolverName}\"");
            if (!string.IsNullOrWhiteSpace(cfg.Namespace)) sb.Append($" -n \"{cfg.Namespace}\"");
            if (!string.IsNullOrWhiteSpace(cfg.MultipleSymbols)) sb.Append($" -ms \"{cfg.MultipleSymbols}\"");

            string arguments = sb.ToString();
            Debug.Log($"<color=cyan>开始生成 [{cfg.Name}]</color>\n执行命令: mpc {arguments}");

            try
            {
                string log = await InvokeProcessStartAsync("mpc", arguments);

                if (!string.IsNullOrWhiteSpace(log) && !log.ToLower().Contains("error"))
                {
                    Debug.Log($"<color=green>【{cfg.Name}】 代码生成成功！</color>\n{log}");
                    return true;
                }
                else
                {
                    Debug.LogError($"<color=red>【{cfg.Name}】 生成报错！</color>\n{log}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成过程发生异常: {ex.Message}");
                return false;
            }
        }

        private Task<string> InvokeProcessStartAsync(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = Application.dataPath
            };

            Process p;
            try
            {
                p = Process.Start(psi);
            }
            catch (Exception ex)
            {
                return Task.FromException<string>(ex);
            }

            var tcs = new TaskCompletionSource<string>();
            p.EnableRaisingEvents = true;
            p.Exited += (object sender, System.EventArgs e) =>
            {
                var data = p.StandardOutput.ReadToEnd();
                var errData = p.StandardError.ReadToEnd();
                p.Dispose();

                if (!string.IsNullOrWhiteSpace(errData))
                {
                    data += "\n[Error Output]:\n" + errData;
                }

                tcs.TrySetResult(data);
            };

            return tcs.Task;
        }

        #endregion
    }

    /// <summary>
    /// MessagePack 编译器配置数据项。
    /// </summary>
    [Serializable]
    public class MpcConfigItem
    {
        public bool IsEnable = true;
        public bool IsExpanded = true;
        public string Name = "New Config";
        public string InputPath = "Scripts";
        public string OutputPath = "Gen/";
        public bool ClearOutputDirectory = true;
        public bool MapMode = false;
        public string Symbols = "";
        public string ResolverName = "GeneratedResolver";
        public string Namespace = "MessagePack.Resolvers";
        public string MultipleSymbols = "";
    }

    /// <summary>
    /// 用于 JsonUtility 序列化的配置列表包装器。
    /// </summary>
    [Serializable]
    public class MpcConfigListWrapper
    {
        public List<MpcConfigItem> Configs = new List<MpcConfigItem>();
    }
}
