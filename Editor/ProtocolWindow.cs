using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GoveKits.Runtime.Network; // 引入你的协议层
using MessagePack;              // 引入 MessagePack

namespace GoveKits.Editor
{
    public class ProtocolWindow : EditorWindow
    {
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
        private List<ProtocolMeta> _protocols = new List<ProtocolMeta>();

        [MenuItem("GoveKits/Protocol", false, 300)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProtocolWindow>("协议字典");
            window.minSize = new Vector2(500, 400);
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
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawProtocolList();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("网络协议 (Protocol) 全局字典", EditorStyles.boldLabel);

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("重新扫描", GUILayout.Width(80), GUILayout.Height(20)))
            {
                ScanProtocols();
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
                meta.IsExpanded = EditorGUILayout.Foldout(meta.IsExpanded, $"[{meta.Id}]  {meta.ClassName}", true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                
                // 拷贝按钮
                if (GUILayout.Button("Copy Name", EditorStyles.miniButton))
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
                        EditorGUILayout.LabelField("  (该消息体没有打上 [Key] 标签的字段/属性)", EditorStyles.miniLabel);
                    }
                    else
                    {
                        // 绘制表头
                        DrawTableRow("Key ID", "数据类型 (Type)", "字段名称 (Name)", true);
                        DrawLine(new Color(0.3f, 0.3f, 0.3f, 0.5f));

                        // 绘制每一行字段
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
            EditorGUILayout.LabelField(col1, style, GUILayout.Width(60));
            EditorGUILayout.LabelField(col2, style, GUILayout.Width(150));
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
                // 过滤掉 Unity 和 System 内部程序集，极大加快扫描速度
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
                    // 应对某些受损程序集抛出的异常
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    // 1. 查找 [ProtocolId] 标签
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

                        // 2. 扫描字段和属性，查找 MessagePack 的 [Key] 标签
                        var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var m in members)
                        {
                            var keyAttr = m.GetCustomAttribute<KeyAttribute>();
                            if (keyAttr != null && keyAttr.IntKey.HasValue) // 确保用的是 Int Key
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

                        // 对内部的 Key 按从小到大排序
                        meta.Members = meta.Members.OrderBy(m => m.KeyId).ToList();
                        
                        _protocols.Add(meta);
                    }
                }
            }

            // 对所有的 Protocol 按 ID 从小到大排序
            _protocols = _protocols.OrderBy(p => p.Id).ToList();
        }

        /// <summary>
        /// 获取友好的类型名称（比如把 System.Single 变成 float）
        /// </summary>
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
            
            // 如果是数组
            if (type.IsArray)
            {
                return GetFriendlyTypeName(type.GetElementType()) + "[]";
            }
            
            return type.Name;
        }

        #endregion
    }
}