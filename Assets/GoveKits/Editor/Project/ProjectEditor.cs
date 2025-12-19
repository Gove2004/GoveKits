using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

namespace GoveKits.Editor
{
    /// <summary>
    /// 项目辅助面板：根据模板创建目录结构、生成 .gitignore，并提供清空 PlayerPrefs 的快捷操作。
    /// </summary>
    public class ProjectEditor : EditorWindow
    {
        private string _directoryPath = Application.dataPath + "Assets/GoveKits/Editor/Project/Template/directory.txt";
        private string _gitignorePath = Application.dataPath + "Assets/GoveKits/Editor/Project/Template/gitignore.txt";
        
        /// <summary>
        /// 打开 Project 面板。
        /// </summary>
        [MenuItem("GoveKits/Project")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectEditor>("Project Editor");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        /// <summary>
        /// 面板 UI 绘制入口。
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            GUILayout.Label("项目结构初始化", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 目录结构初始化部分
            DrawDirectorySection();
            EditorGUILayout.Space(20);

            // .gitignore 创建部分
            DrawGitIgnoreSection();

            // 一键清除PlayerPrefs按钮
            EditorGUILayout.Space(20);
            if (GUILayout.Button("清除 PlayerPrefs", GUILayout.Height(30)))
            {
                PlayerPrefs.DeleteAll();
                ShowNotification(new GUIContent("已清除 PlayerPrefs"));
                LogManager.Log("ProjectEditor", "已清除 PlayerPrefs");
            }
        }
        
        /// <summary>
        /// 绘制“目录结构初始化”区域。
        /// </summary>
        private void DrawDirectorySection()
        {
            EditorGUILayout.LabelField("目录结构", EditorStyles.boldLabel);
            
            // 目录模板文件路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("模板文件:", GUILayout.Width(80));
            _directoryPath = EditorGUILayout.TextField(_directoryPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择目录模板文件", 
                    Path.GetDirectoryName(_directoryPath), "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    _directoryPath = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 文件验证
            if (!File.Exists(_directoryPath))
            {
                EditorGUILayout.HelpBox("模板文件不存在", MessageType.Warning);
            }
            else
            {
                string[] directories = GetDirectoriesFromFile(_directoryPath);
                EditorGUILayout.HelpBox($"将创建 {directories.Length} 个目录", MessageType.Info);
            }
            
            // 初始化按钮
            GUI.enabled = File.Exists(_directoryPath);
            if (GUILayout.Button("初始化项目结构", GUILayout.Height(30)))
            {
                InitializeProject();
            }
            GUI.enabled = true;
        }
        
        /// <summary>
        /// 绘制“.gitignore 创建”区域。
        /// </summary>
        private void DrawGitIgnoreSection()
        {
            EditorGUILayout.LabelField(".gitignore", EditorStyles.boldLabel);
            
            // .gitignore 模板文件路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("模板文件:", GUILayout.Width(80));
            _gitignorePath = EditorGUILayout.TextField(_gitignorePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("选择 .gitignore 模板文件", 
                    Path.GetDirectoryName(_gitignorePath), "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    _gitignorePath = FileUtil.GetProjectRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 文件验证
            if (!File.Exists(_gitignorePath))
            {
                EditorGUILayout.HelpBox("模板文件不存在", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("模板文件已找到", MessageType.Info);
            }
            
            // 创建按钮
            GUI.enabled = File.Exists(_gitignorePath);
            if (GUILayout.Button("创建 .gitignore", GUILayout.Height(30)))
            {
                CreateGitIgnore();
            }
            GUI.enabled = true;
        }
        
        /// <summary>
        /// 从模板文件中解析目录列表（忽略空行与以#开头的注释）。
        /// </summary>
        /// <param name="filePath">模板文件路径。</param>
        /// <returns>目录相对路径数组。</returns>
        private string[] GetDirectoriesFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return new string[0];
            
            string content = File.ReadAllText(filePath, Encoding.UTF8);
            return content.Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                .ToArray();
        }
        
        /// <summary>
        /// 根据模板创建缺失目录，并刷新资源。
        /// </summary>
        private void InitializeProject()
        {
            string[] directories = GetDirectoriesFromFile(_directoryPath);
            
            int created = 0;
            foreach (var path in directories)
            {
                if (!Directory.Exists(path))
                {
                    LogManager.LogGreen("ProjectEditor", $"创建目录: {path}");
                    Directory.CreateDirectory(path);
                    created++;
                }
            }
            
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent($"创建了 {created} 个目录"));
            LogManager.LogGreen("ProjectEditor", $"创建了 {created} 个目录");
        }
        
        /// <summary>
        /// 根据模板生成 .gitignore 文件到项目根目录。
        /// </summary>
        private void CreateGitIgnore()
        {
            string gitignoreContent = File.ReadAllText(_gitignorePath, Encoding.UTF8);
            string fullPath = Path.Combine(Application.dataPath, "../.gitignore");
            File.WriteAllText(fullPath, gitignoreContent, Encoding.UTF8);
            
            ShowNotification(new GUIContent("已创建 .gitignore 文件"));
            LogManager.LogGreen("ProjectEditor", "已创建 .gitignore 文件");
        }
        
        /// <summary>
        /// 载入面板配置。
        /// </summary>
        private void OnEnable()
        {
            _directoryPath = EditorPrefs.GetString("ProjectEditor.DirectoryPath", _directoryPath);
            _gitignorePath = EditorPrefs.GetString("ProjectEditor.GitIgnorePath", _gitignorePath);
        }
        
        /// <summary>
        /// 保存面板配置。
        /// </summary>
        private void OnDisable()
        {
            EditorPrefs.SetString("ProjectEditor.DirectoryPath", _directoryPath);
            EditorPrefs.SetString("ProjectEditor.GitIgnorePath", _gitignorePath);
        }
    }
}