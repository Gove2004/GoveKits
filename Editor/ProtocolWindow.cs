using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using GoveKits.Runtime.Network; // 引入你的协议层
using MessagePack;              // 引入 MessagePack

namespace GoveKits.Editor
{
    public class ProtocolWindow : EditorWindow
    {
        private enum ExportFormat
        {
            Markdown,
            JSON,
            PlainText
        }

        // === 数据结构 ===
        private class ProtocolMemberMeta
        {
            public int KeyId;
            public string TypeName;
            public string MemberName;
        }

        private class ProtocolMeta
        {
            public ushort Id;
            public string ClassName;
            public string FullName;
            public bool IsExpanded;
            public List<ProtocolMemberMeta> Members = new List<ProtocolMemberMeta>();
        }

        private Vector2 _scrollPos;
        private string _searchQuery = string.Empty;
        private ExportFormat _exportFormat = ExportFormat.Markdown;
        private List<ProtocolMeta> _protocols = new List<ProtocolMeta>();

        [MenuItem("GoveKits/Protocol", false, 300)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProtocolWindow>("协议字典");
            window.minSize = new Vector2(550, 500);
            window.Show();
        }

        private void OnEnable()
        {
            ScanProtocols();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawExportSection();
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawProtocolList();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("网络协议 (Protocol) 全局字典", EditorStyles.largeLabel);

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("重新扫描", GUILayout.Width(90), GUILayout.Height(24)))
            {
                ScanProtocols();
                ShowNotification(new GUIContent($"扫描完成，共 {_protocols.Count} 条协议"));
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        private void DrawToolbar()
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal("box");
            
            EditorGUIUtility.labelWidth = 40;
            _searchQuery = EditorGUILayout.TextField("搜索:", _searchQuery);
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(10);

            if (GUILayout.Button("展开全部", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                _protocols.ForEach(p => p.IsExpanded = true);
            }
            if (GUILayout.Button("收起全部", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                _protocols.ForEach(p => p.IsExpanded = false);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawExportSection()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("导出协议文档:", EditorStyles.boldLabel, GUILayout.Width(90));
            
            _exportFormat = (ExportFormat)EditorGUILayout.EnumPopup(_exportFormat, GUILayout.Width(120));
            
            GUILayout.FlexibleSpace();

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button(" 一键导出文档 ", GUILayout.Width(120), GUILayout.Height(22)))
            {
                ExecuteExport();
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void DrawProtocolList()
        {
            if (_protocols.Count == 0)
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("未扫描到任何带有 [ProtocolId] 的协议类，请检查代码或点击重新扫描。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"共找到 {_protocols.Count} 个协议消息", EditorStyles.miniLabel);
            GUILayout.Space(5);

            foreach (var meta in _protocols)
            {
                // 搜索过滤：支持按 ID、类名 搜索
                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    bool matchId = meta.Id.ToString().Contains(_searchQuery);
                    bool matchName = meta.ClassName.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!matchId && !matchName) continue;
                }

                EditorGUILayout.BeginVertical("helpbox");

                // --- 标题行 ---
                EditorGUILayout.BeginHorizontal();
                GUIContent icon = EditorGUIUtility.IconContent("Assembly Icon");
                icon.text = $" [{meta.Id}]  {meta.ClassName}";
                
                meta.IsExpanded = EditorGUILayout.Foldout(meta.IsExpanded, icon, true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    GUIUtility.systemCopyBuffer = meta.ClassName;
                    ShowNotification(new GUIContent($"已复制: {meta.ClassName}"));
                }
                EditorGUILayout.EndHorizontal();

                // --- 详情内容 (展开后) ---
                if (meta.IsExpanded)
                {
                    GUILayout.Space(5);
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField("命名空间:", meta.FullName.Replace("." + meta.ClassName, ""), EditorStyles.miniLabel);
                    GUILayout.Space(5);

                    if (meta.Members.Count == 0)
                    {
                        EditorGUILayout.LabelField("  (该消息体没有打上 [Key] 标签的字段/属性)", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        DrawTableRow("Key ID", "数据类型 (Type)", "字段名称 (Name)", true);
                        DrawLine(new Color(0.3f, 0.3f, 0.3f, 0.5f));

                        foreach (var m in meta.Members)
                        {
                            DrawTableRow($"[{m.KeyId}]", m.TypeName, m.MemberName, false);
                        }
                    }

                    EditorGUI.indentLevel--;
                    GUILayout.Space(5);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        private void DrawTableRow(string col1, string col2, string col3, bool isHeader)
        {
            GUIStyle style = isHeader ? EditorStyles.boldLabel : EditorStyles.label;
            
            EditorGUILayout.BeginHorizontal();
            
            var defaultColor = GUI.contentColor;
            if (!isHeader) GUI.contentColor = new Color(1f, 0.7f, 0.3f); // 把 KeyId 标成橘黄色
            EditorGUILayout.LabelField(col1, style, GUILayout.Width(60));
            
            if (!isHeader) GUI.contentColor = new Color(0.4f, 0.8f, 1f); // 把 Type 标成浅蓝色
            EditorGUILayout.LabelField(col2, style, GUILayout.Width(150));
            
            GUI.contentColor = defaultColor;
            EditorGUILayout.LabelField(col3, style);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLine(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        #region 核心扫描逻辑

        private void ScanProtocols()
        {
            _protocols.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // 过滤掉内部程序集
                string assName = assembly.FullName;
                if (assName.StartsWith("Unity") || assName.StartsWith("System") || assName.StartsWith("mscorlib") || assName.StartsWith("MessagePack"))
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    var protocolAttr = type.GetCustomAttribute<ProtocolIdAttribute>();
                    if (protocolAttr != null)
                    {
                        var meta = new ProtocolMeta
                        {
                            Id = protocolAttr.Id,
                            ClassName = type.Name,
                            FullName = type.FullName,
                            IsExpanded = false
                        };

                        var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var m in members)
                        {
                            var keyAttr = m.GetCustomAttribute<KeyAttribute>();
                            if (keyAttr != null && keyAttr.IntKey.HasValue) 
                            {
                                string typeName = "Unknown";
                                
                                if (m is FieldInfo fi) typeName = GetFriendlyTypeName(fi.FieldType);
                                else if (m is PropertyInfo pi) typeName = GetFriendlyTypeName(pi.PropertyType);

                                meta.Members.Add(new ProtocolMemberMeta
                                {
                                    KeyId = keyAttr.IntKey.Value,
                                    MemberName = m.Name,
                                    TypeName = typeName
                                });
                            }
                        }

                        meta.Members = meta.Members.OrderBy(m => m.KeyId).ToList();
                        _protocols.Add(meta);
                    }
                }
            }

            _protocols = _protocols.OrderBy(p => p.Id).ToList();
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(short)) return "short";
            if (type == typeof(long)) return "long";
            if (type == typeof(double)) return "double";
            
            if (type.IsArray)
            {
                return GetFriendlyTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return $"List<{GetFriendlyTypeName(type.GetGenericArguments()[0])}>";
            }
            
            return type.Name;
        }

        #endregion

        #region 一键导出逻辑

        private void ExecuteExport()
        {
            if (_protocols.Count == 0)
            {
                ShowNotification(new GUIContent("没有任何协议可导出，请先扫描！"));
                return;
            }

            string extension = _exportFormat == ExportFormat.Markdown ? "md" : 
                               _exportFormat == ExportFormat.JSON ? "json" : "txt";

            string path = EditorUtility.SaveFilePanel(
                "导出协议文档", 
                Application.dataPath, 
                $"Protocol_Dict_{DateTime.Now:yyyyMMdd}.{extension}", 
                extension);

            if (string.IsNullOrEmpty(path)) return;

            string content = "";

            switch (_exportFormat)
            {
                case ExportFormat.Markdown:
                    content = GenerateMarkdown();
                    break;
                case ExportFormat.JSON:
                    content = GenerateJSON();
                    break;
                case ExportFormat.PlainText:
                    content = GeneratePlainText();
                    break;
            }

            try
            {
                File.WriteAllText(path, content, Encoding.UTF8);
                EditorUtility.RevealInFinder(path);
                Debug.Log($"<color=green>✅ 协议文档导出成功: {path}</color>");
            }
            catch (Exception e)
            {
                Debug.LogError($"导出失败: {e.Message}");
            }
        }

        private string GenerateMarkdown()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# GoveKits 网络协议文档");
            sb.AppendLine($"> 自动生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"> 协议总数：{_protocols.Count}\n");

            foreach (var p in _protocols)
            {
                sb.AppendLine($"## [{p.Id}] {p.ClassName}");
                sb.AppendLine($"- **Namespace**: `{p.FullName}`");
                sb.AppendLine();

                if (p.Members.Count == 0)
                {
                    sb.AppendLine("*无 Payload 数据*");
                    sb.AppendLine();
                    continue;
                }

                sb.AppendLine("| KeyId | Type | FieldName |");
                sb.AppendLine("| :---: | :--- | :--- |");
                
                foreach (var m in p.Members)
                {
                    sb.AppendLine($"| {m.KeyId} | `{m.TypeName}` | {m.MemberName} |");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GeneratePlainText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================ GoveKits Protocol Dict ================");
            sb.AppendLine($"Total: {_protocols.Count} | Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            foreach (var p in _protocols)
            {
                sb.AppendLine($"[{p.Id}] {p.ClassName}");
                foreach (var m in p.Members)
                {
                    sb.AppendLine($"    -> Key: {m.KeyId,-3} | Type: {m.TypeName,-12} | Name: {m.MemberName}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateJSON()
        {
            // 为避免引入第三方 JSON 库的依赖，手动拼接极简 JSON，保证兼容性
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
            sb.AppendLine($"  \"total\": {_protocols.Count},");
            sb.AppendLine("  \"protocols\": [");

            for (int i = 0; i < _protocols.Count; i++)
            {
                var p = _protocols[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"id\": {p.Id},");
                sb.AppendLine($"      \"className\": \"{p.ClassName}\",");
                sb.AppendLine($"      \"namespace\": \"{p.FullName.Replace("." + p.ClassName, "")}\",");
                sb.AppendLine("      \"members\": [");

                for (int j = 0; j < p.Members.Count; j++)
                {
                    var m = p.Members[j];
                    sb.AppendLine("        {");
                    sb.AppendLine($"          \"keyId\": {m.KeyId},");
                    sb.AppendLine($"          \"type\": \"{m.TypeName}\",");
                    sb.AppendLine($"          \"name\": \"{m.MemberName}\"");
                    sb.Append("        }");
                    if (j < p.Members.Count - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }

                sb.AppendLine("      ]");
                sb.Append("    }");
                if (i < _protocols.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        #endregion
    }
}