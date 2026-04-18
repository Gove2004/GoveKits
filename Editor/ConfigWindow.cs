using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GoveKits.Runtime.Storage;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class ConfigWindow : EditorWindow
    {
        private enum ViewMode { List, Folder }

        private class ConfigBindingView
        {
            public Type ConfigType;
            public string FilePath;     
            public string EditorAssetPath; 
            public bool ExistsInEditor;
            public int LoadedRows = -1; 
        }

        private Vector2 _scrollPos;
        private ViewMode _viewMode = ViewMode.List;
        private string _searchQuery = string.Empty;
        private bool _onlyShowMissing = false;

        private readonly List<ConfigBindingView> _bindings = new();
        private readonly Dictionary<string, List<ConfigBindingView>> _folderGroups = new();
        
        // 记录哪些配置表被展开以查看数据结构
        private readonly HashSet<Type> _expandedTypes = new();

        [MenuItem("GoveKits/Config", false, 202)]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigWindow>("Config 管理");
            window.minSize = new Vector2(500, 550);
            window.Show();
        }

        private void OnEnable()
        {
            ScanBindingsViaReflection();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            if (_bindings.Count == 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("未扫描到带有 [ConfigPath] 的 IConfigData 配置类。", MessageType.Info);
            }
            else
            {
                if (_viewMode == ViewMode.List) DrawListView();
                else DrawFolderView();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Config 配置表映射管理", EditorStyles.boldLabel);
            
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("Copy Report", GUILayout.Width(100), GUILayout.Height(20))) CopyReport();
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal("box");
            _viewMode = (ViewMode)GUILayout.Toolbar((int)_viewMode, new[] { "列表视图", "文件夹视图" }, GUILayout.Width(200), GUILayout.Height(22));
            GUILayout.FlexibleSpace();
            _onlyShowMissing = EditorGUILayout.ToggleLeft("仅显示缺失", _onlyShowMissing, GUILayout.Width(90));
            if (GUILayout.Button("重新扫描", EditorStyles.miniButton, GUILayout.Width(70))) ScanBindingsViaReflection();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            EditorGUIUtility.labelWidth = 40;
            _searchQuery = EditorGUILayout.TextField("搜索:", _searchQuery);
            EditorGUIUtility.labelWidth = 0;

            if (Application.isPlaying)
            {
                if (GUILayout.Button("刷新内存行数", EditorStyles.miniButton, GUILayout.Width(100))) RefreshLoadedRowsViaReflection();
            }
            EditorGUILayout.EndHorizontal();

            DrawSummary();
        }

        private void DrawSummary()
        {
            int missingCount = _bindings.Count(b => !b.ExistsInEditor);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"绑定数: {_bindings.Count} | 已加载: {_bindings.Count(b => b.LoadedRows >= 0)}", EditorStyles.miniLabel);
            
            var defaultColor = GUI.contentColor;
            if (missingCount > 0) GUI.contentColor = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label($"缺失: {missingCount}", EditorStyles.miniLabel);
            GUI.contentColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawListView()
        {
            foreach (var binding in _bindings)
            {
                if (_onlyShowMissing && binding.ExistsInEditor) continue;
                if (!string.IsNullOrEmpty(_searchQuery) && binding.ConfigType.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;

                DrawConfigCard(binding);
            }
        }

        private void DrawFolderView()
        {
            foreach (var kvp in _folderGroups)
            {
                string folder = string.IsNullOrEmpty(kvp.Key) ? "<根目录>" : kvp.Key;
                var list = kvp.Value.Where(b => 
                    (!_onlyShowMissing || !b.ExistsInEditor) && 
                    (string.IsNullOrEmpty(_searchQuery) || b.ConfigType.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();

                if (list.Count == 0) continue;

                EditorGUILayout.BeginVertical("box");
                GUILayout.Label($"📂 {folder} ({list.Count})", EditorStyles.boldLabel);
                DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.2f));
                
                foreach (var binding in list) DrawConfigCard(binding);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawConfigCard(ConfigBindingView binding)
        {
            EditorGUILayout.BeginVertical("helpbox");

            // --- 标题折叠栏 ---
            EditorGUILayout.BeginHorizontal();
            
            bool isExpanded = _expandedTypes.Contains(binding.ConfigType);
            isExpanded = EditorGUILayout.Foldout(isExpanded, binding.ConfigType.Name, true, EditorStyles.foldoutHeader);
            if (isExpanded) _expandedTypes.Add(binding.ConfigType);
            else _expandedTypes.Remove(binding.ConfigType);

            GUILayout.FlexibleSpace();

            // 状态文字
            var defaultColor = GUI.contentColor;
            if (!binding.ExistsInEditor)
            {
                GUI.contentColor = new Color(0.9f, 0.3f, 0.3f);
                GUILayout.Label("缺失", EditorStyles.boldLabel, GUILayout.Width(35));
            }
            else
            {
                GUI.contentColor = new Color(0.3f, 0.8f, 0.3f);
                GUILayout.Label("正常", EditorStyles.boldLabel, GUILayout.Width(35));
            }
            GUI.contentColor = defaultColor;

            EditorGUILayout.EndHorizontal();

            // --- 信息栏 ---
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(binding.FilePath, EditorStyles.miniLabel);

            if (binding.LoadedRows >= 0)
            {
                GUI.contentColor = new Color(0.4f, 0.8f, 1f);
                GUILayout.Label($"已加载: {binding.LoadedRows} 行", EditorStyles.miniLabel, GUILayout.Width(100));
                GUI.contentColor = defaultColor;
            }

            GUI.enabled = binding.ExistsInEditor;
            if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(binding.EditorAssetPath);
                if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // --- 展开数据结构 (黑科技) ---
            if (isExpanded)
            {
                DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.2f));
                DrawDataStructure(binding.ConfigType);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void DrawDataStructure(Type type)
        {
            // 获取所有的 Public 字段和属性
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            int count = 0;

            foreach (var member in members)
            {
                Type memberType = null;
                string memberName = member.Name;

                if (member.MemberType == MemberTypes.Field)
                {
                    memberType = ((FieldInfo)member).FieldType;
                }
                else if (member.MemberType == MemberTypes.Property)
                {
                    var prop = (PropertyInfo)member;
                    // 跳过索引器
                    if (prop.GetIndexParameters().Length == 0) memberType = prop.PropertyType;
                }

                if (memberType != null)
                {
                    string niceTypeName = GetNiceTypeName(memberType);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(15); // 缩进
                    // 类型浅蓝色，变量名白色
                    var defaultColor = GUI.contentColor;
                    GUI.contentColor = new Color(0.4f, 0.8f, 1f);
                    GUILayout.Label(niceTypeName, EditorStyles.miniLabel, GUILayout.Width(120));
                    GUI.contentColor = defaultColor;
                    GUILayout.Label(memberName, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    count++;
                }
            }

            if (count == 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label("无公开的数据成员", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        // 将 System.Int32 格式化为 int，List`1 格式化为 List<T> 的辅助方法
        private string GetNiceTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(long)) return "long";

            if (type.IsGenericType)
            {
                string genericName = type.Name.Split('`')[0];
                var args = type.GetGenericArguments();
                string argsStr = string.Join(", ", args.Select(GetNiceTypeName));
                return $"{genericName}<{argsStr}>";
            }
            if (type.IsArray)
            {
                return $"{GetNiceTypeName(type.GetElementType())}[]";
            }

            return type.Name;
        }

        private void DrawLine(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        #region 反射扫描逻辑

        private void ScanBindingsViaReflection()
        {
            _bindings.Clear();
            _folderGroups.Clear();

            try
            {
                Type scannerType = typeof(ConfigCore).Assembly.GetType("GoveKits.Runtime.Storage.ConfigBindingScanner");
                if (scannerType == null) return;

                MethodInfo scanMethod = scannerType.GetMethod("Scan", BindingFlags.Public | BindingFlags.Static);
                if (scanMethod == null) return;

                IList scanResults = scanMethod.Invoke(null, null) as IList;
                if (scanResults == null) return;

                foreach (object bindingObj in scanResults)
                {
                    Type bindingStructType = bindingObj.GetType();
                    Type configType = bindingStructType.GetProperty("ConfigType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(bindingObj) as Type;
                    object attrObj = bindingStructType.GetProperty("Attribute", BindingFlags.Public | BindingFlags.Instance)?.GetValue(bindingObj);
                    string filePath = attrObj?.GetType().GetProperty("FilePath", BindingFlags.Public | BindingFlags.Instance)?.GetValue(attrObj) as string;

                    if (configType == null || string.IsNullOrEmpty(filePath)) continue;

                    var view = new ConfigBindingView { ConfigType = configType, FilePath = filePath, LoadedRows = -1 };
                    ResolveEditorAssetExistence(view);

                    _bindings.Add(view);

                    string folder = Path.GetDirectoryName(view.FilePath.Replace('\\', '/'));
                    if (!_folderGroups.TryGetValue(folder, out var list))
                    {
                        list = new List<ConfigBindingView>();
                        _folderGroups[folder] = list;
                    }
                    list.Add(view);
                }

                _bindings.Sort((a, b) => string.Compare(a.ConfigType.Name, b.ConfigType.Name, StringComparison.Ordinal));
            }
            catch (Exception ex) { Debug.LogError($"扫描配置表失败: {ex}"); }

            if (Application.isPlaying) RefreshLoadedRowsViaReflection();
        }

        private void ResolveEditorAssetExistence(ConfigBindingView view)
        {
            view.ExistsInEditor = false;
            string fileName = Path.GetFileName(view.FilePath);
            int colonIndex = fileName.LastIndexOf(':');
            if (colonIndex >= 0) fileName = fileName.Substring(colonIndex + 1);

            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string[] guids = AssetDatabase.FindAssets($"{nameWithoutExt} t:TextAsset");
            
            if (guids.Length > 0)
            {
                string expectedDir = Path.GetDirectoryName(view.FilePath.Replace('\\', '/'));
                if (colonIndex >= 0) expectedDir = expectedDir.Substring(colonIndex + 1);
                
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(expectedDir) || assetPath.Replace('\\', '/').Contains(expectedDir))
                    {
                        view.ExistsInEditor = true;
                        view.EditorAssetPath = assetPath;
                        break;
                    }
                }
                if (!view.ExistsInEditor) { view.ExistsInEditor = true; view.EditorAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]); }
            }
        }

        private void RefreshLoadedRowsViaReflection()
        {
            try
            {
                FieldInfo tablesField = typeof(ConfigCore).GetField("_configTables", BindingFlags.NonPublic | BindingFlags.Static);
                if (tablesField == null) return;
                var tables = tablesField.GetValue(null) as IDictionary;
                if (tables == null) return;

                foreach (var binding in _bindings)
                {
                    if (tables.Contains(binding.ConfigType))
                    {
                        var list = tables[binding.ConfigType] as IList;
                        binding.LoadedRows = list != null ? list.Count : 0;
                    }
                    else binding.LoadedRows = -1;
                }
            }
            catch { }
        }

        private void CopyReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Config Manager Report ===");
            foreach (var b in _bindings)
            {
                string status = b.ExistsInEditor ? "OK" : "MISSING";
                sb.AppendLine($"[{status}] {b.ConfigType.Name} | Path: {b.FilePath}");
            }
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            ShowNotification(new GUIContent("报告已复制！"));
        }

        #endregion
    }
}