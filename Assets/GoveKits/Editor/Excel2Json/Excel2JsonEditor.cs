using System.IO;
using UnityEditor;
using UnityEngine;
using GoveKits.Utility;

namespace GoveKits.Tool
{
    /// <summary>
    /// Excel转JSON编辑器工具窗口
    /// </summary>
    public class Excel2JsonEditor : EditorWindow
    {
        [Header("路径设置")]
        [SerializeField] private string inputPath = "Assets/Config/Excel";
        [SerializeField] private string outputPath = "Assets/Config/Json";
        [SerializeField] private string keyColumn = "key";
        
        [Header("Python设置")]
        [SerializeField] private string pythonPath = "python";
        [SerializeField] private string scriptPath = "Assets/GoveKits/Editor/Excel2Json/excel2json.py";
        
        private Vector2 scrollPosition;
        private Vector2 logScrollPosition; // 新增：日志滚动位置
        private bool isConverting = false;
        private string lastConvertResult = "";
        private int successFiles = 0;
        private int failedFiles = 0;
        
        /// <summary>
        /// 创建菜单项
        /// </summary>
        [MenuItem("GoveKits/Excel2Json")]
        public static void ShowWindow()
        {
            Excel2JsonEditor window = GetWindow<Excel2JsonEditor>("Excel2Json 转换器");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.Space(10);
            
            // 标题
            GUILayout.Label("Excel2Json 转换工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 路径设置区域
            DrawPathSettings();
            EditorGUILayout.Space(10);
            
            // Python设置区域
            DrawPythonSettings();
            EditorGUILayout.Space(10);
            
            // 操作按钮区域
            DrawActionButtons();
            EditorGUILayout.Space(10);
            
            // 结果显示区域
            DrawResultArea();
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 绘制路径设置区域
        /// </summary>
        private void DrawPathSettings()
        {
            EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
            
            // 输入路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输入路径:", GUILayout.Width(80));
            inputPath = EditorGUILayout.TextField(inputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择Excel文件目录", inputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    inputPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 输出路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出路径:", GUILayout.Width(80));
            outputPath = EditorGUILayout.TextField(outputPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择JSON输出目录", outputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    outputPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 键名列
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("键名列:", GUILayout.Width(80));
            keyColumn = EditorGUILayout.TextField(keyColumn);
            EditorGUILayout.EndHorizontal();
            
            // 显示文件数量
            if (Directory.Exists(inputPath))
            {
                var excelFiles = GetExcelFiles();
                EditorGUILayout.HelpBox($"发现 {excelFiles.Length} 个Excel文件", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("输入目录不存在", MessageType.Warning);
            }
        }
        
        /// <summary>
        /// 绘制Python设置区域
        /// </summary>
        private void DrawPythonSettings()
        {
            EditorGUILayout.LabelField("Python设置", EditorStyles.boldLabel);
            
            // Python路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Python命令:", GUILayout.Width(80));
            pythonPath = EditorGUILayout.TextField(pythonPath);
            EditorGUILayout.EndHorizontal();
            
            // 脚本路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("脚本路径:", GUILayout.Width(80));
            scriptPath = EditorGUILayout.TextField(scriptPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("选择Python脚本", 
                    Path.GetDirectoryName(scriptPath), "py");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    scriptPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 脚本验证
            if (!File.Exists(scriptPath))
            {
                EditorGUILayout.HelpBox("Python脚本不存在", MessageType.Error);
            }
        }
        
        /// <summary>
        /// 绘制操作按钮区域
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            
            GUI.enabled = !isConverting && File.Exists(scriptPath) && Directory.Exists(inputPath);
            
            if (GUILayout.Button("开始转换", GUILayout.Height(30)))
            {
                StartConversion();
            }
            
            GUI.enabled = true;
        }
        
        /// <summary>
        /// 绘制结果显示区域
        /// </summary>
        private void DrawResultArea()
        {
            EditorGUILayout.LabelField("转换结果", EditorStyles.boldLabel);
            
            if (isConverting)
            {
                EditorGUILayout.HelpBox("正在转换中，请稍候...", MessageType.Info);
                
                // 动态进度条
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                float progress = Mathf.PingPong(Time.realtimeSinceStartup * 0.5f, 1f);
                EditorGUI.ProgressBar(rect, progress, "转换中...");
                
                Repaint();
            }
            else if (!string.IsNullOrEmpty(lastConvertResult))
            {
                // 转换结果摘要
                if (successFiles > 0 || failedFiles > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("转换完成", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"成功: {successFiles}");
                    EditorGUILayout.LabelField($"失败: {failedFiles}");
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
                
                // 详细日志（带滚动）
                EditorGUILayout.LabelField("详细日志:", EditorStyles.boldLabel);
                
                // 使用滚动视图包装日志文本区域
                logScrollPosition = EditorGUILayout.BeginScrollView(
                    logScrollPosition, 
                    GUILayout.MinHeight(150), 
                    GUILayout.MaxHeight(300)
                );
                
                EditorGUILayout.TextArea(lastConvertResult, GUILayout.ExpandHeight(true));
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("等待开始转换...", MessageType.None);
            }
        }
        
        /// <summary>
        /// 获取Excel文件列表
        /// </summary>
        private string[] GetExcelFiles()
        {
            if (!Directory.Exists(inputPath))
                return new string[0];
                
            var files = new System.Collections.Generic.List<string>();
            string[] extensions = { "*.xlsx", "*.xls", "*.xlsm" };
            
            foreach (string ext in extensions)
            {
                files.AddRange(Directory.GetFiles(inputPath, ext, SearchOption.AllDirectories));
            }
            
            return files.ToArray();
        }
        
        /// <summary>
        /// 开始转换
        /// </summary>
        private void StartConversion()
        {
            if (isConverting) return;
            
            isConverting = true;
            lastConvertResult = "";
            successFiles = 0;
            failedFiles = 0;
            
            // 重置日志滚动位置
            logScrollPosition = Vector2.zero;
            
            // 构建命令参数
            string args = $"\"{scriptPath}\" -i \"{inputPath}\" -o \"{outputPath}\" -k \"{keyColumn}\"";
            
            Debug.Log($"[Excel2Json] 开始转换: {pythonPath} {args}");
            
            // 异步执行
            System.Threading.Tasks.Task.Run(() =>
            {
                string[] result = CMD.ExecuteWithError(pythonPath, args);
                
                // 回到主线程
                EditorApplication.delayCall += () =>
                {
                    OnConversionComplete(result);
                };
            });
        }
        
        /// <summary>
        /// 转换完成回调
        /// </summary>
        private void OnConversionComplete(string[] result)
        {
            isConverting = false;
            
            string output = result[0];
            string error = result[1];
            string exitCode = result[2];
            
            // 解析统计信息
            ParseConversionOutput(output);
            
            // 组合结果
            lastConvertResult = $"退出码: {exitCode}\n转换时间: {System.DateTime.Now:HH:mm:ss}\n\n";
            
            if (!string.IsNullOrEmpty(output))
            {
                lastConvertResult += $"输出:\n{output}\n\n";
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                lastConvertResult += $"错误:\n{error}";
            }
            
            // 显示结果
            bool success = exitCode == "0";
            if (success)
            {
                ShowNotification(new GUIContent($"转换完成! 成功:{successFiles} 失败:{failedFiles}"));
                AssetDatabase.Refresh();
            }
            else
            {
                ShowNotification(new GUIContent("转换失败!"));
            }
            
            Repaint();
        }
        
        /// <summary>
        /// 解析转换输出获取统计信息
        /// </summary>
        private void ParseConversionOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return;
            
            string[] lines = output.Split('\n');
            
            foreach (string line in lines)
            {
                if (line.Contains("[SUCCESS] 成功:") && line.Contains("个文件"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"成功:\s*(\d+)\s*个文件");
                    if (match.Success)
                    {
                        int.TryParse(match.Groups[1].Value, out successFiles);
                    }
                }
                
                if (line.Contains("[FAILED] 失败:") && line.Contains("个文件"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"失败:\s*(\d+)\s*个文件");
                    if (match.Success)
                    {
                        int.TryParse(match.Groups[1].Value, out failedFiles);
                    }
                }
            }
        }
        
        /// <summary>
        /// 保存设置
        /// </summary>
        private void OnDisable()
        {
            EditorPrefs.SetString("Excel2Json.InputPath", inputPath);
            EditorPrefs.SetString("Excel2Json.OutputPath", outputPath);
            EditorPrefs.SetString("Excel2Json.KeyColumn", keyColumn);
            EditorPrefs.SetString("Excel2Json.PythonPath", pythonPath);
            EditorPrefs.SetString("Excel2Json.ScriptPath", scriptPath);
        }
        
        /// <summary>
        /// 加载设置
        /// </summary>
        private void OnEnable()
        {
            inputPath = EditorPrefs.GetString("Excel2Json.InputPath", inputPath);
            outputPath = EditorPrefs.GetString("Excel2Json.OutputPath", outputPath);
            keyColumn = EditorPrefs.GetString("Excel2Json.KeyColumn", keyColumn);
            pythonPath = EditorPrefs.GetString("Excel2Json.PythonPath", pythonPath);
            scriptPath = EditorPrefs.GetString("Excel2Json.ScriptPath", scriptPath);
        }
    }
}