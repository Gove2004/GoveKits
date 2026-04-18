using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoveKits.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class EventWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _searchQuery = string.Empty;
        
        private bool _autoRefresh = true;
        private double _nextRefreshTime;
        private const double RefreshInterval = 0.5;

        // 反射抓取用的 FieldInfo
        private FieldInfo _busesField;
        private FieldInfo _listenerMapsField;

        // UI 状态缓存
        private string _selectedBusName = EventCore.DefaultBusName;
        private readonly HashSet<Type> _expandedEventTypes = new();

        [MenuItem("GoveKits/Event", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<EventWindow>("Event 监控");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // 初始化反射信息
            _busesField = typeof(EventCore).GetField("_buses", BindingFlags.NonPublic | BindingFlags.Static);
            _listenerMapsField = typeof(EventBus).GetField("_listenerMaps", BindingFlags.NonPublic | BindingFlags.Instance);

            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;

            if (EditorApplication.timeSinceStartup > _nextRefreshTime)
            {
                _nextRefreshTime = EditorApplication.timeSinceStartup + RefreshInterval;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            if (!Application.isPlaying)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("事件总线监控需要在 Play 模式 (运行状态) 下才能抓取实时内存数据。", MessageType.Info);
            }
            else
            {
                DrawActiveBusData();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("EventBus 内存全景监控", EditorStyles.largeLabel);

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("一键清空所有频道", GUILayout.Width(130), GUILayout.Height(24)))
            {
                EventCore.Clear();
                Repaint();
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal("helpbox");
            
            EditorGUIUtility.labelWidth = 60;
            _searchQuery = EditorGUILayout.TextField("搜索事件:", _searchQuery);
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(10);
            _autoRefresh = EditorGUILayout.ToggleLeft("自动刷新 (0.5s)", _autoRefresh, GUILayout.Width(120));
            if (GUILayout.Button("手动刷新", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        #region 反射与核心绘制

        private void DrawActiveBusData()
        {
            if (_busesField == null) return;
            
            // 1. 抓取所有的 Bus
            var buses = _busesField.GetValue(null) as Dictionary<string, EventBus>;
            if (buses == null || buses.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有任何活跃的 EventBus 频道。", MessageType.Info);
                return;
            }

            // 2. 绘制 Bus 切换页签
            List<string> busNames = new List<string>(buses.Keys);
            if (!busNames.Contains(_selectedBusName) && busNames.Count > 0)
            {
                _selectedBusName = busNames[0];
            }

            int selectedIndex = Mathf.Max(0, busNames.IndexOf(_selectedBusName));
            
            GUILayout.Space(5);
            int newIndex = GUILayout.Toolbar(selectedIndex, busNames.ToArray(), GUILayout.Height(26));
            _selectedBusName = busNames[newIndex];
            GUILayout.Space(10);

            // 3. 抓取当前选中 Bus 下的所有事件映射
            if (!buses.TryGetValue(_selectedBusName, out EventBus currentBus)) return;
            if (_listenerMapsField == null) return;

            var listenerMaps = _listenerMapsField.GetValue(currentBus) as Dictionary<Type, object>;
            if (listenerMaps == null || listenerMaps.Count == 0)
            {
                EditorGUILayout.HelpBox($"频道 [{_selectedBusName}] 当前没有任何事件订阅。", MessageType.Info);
                return;
            }

            int totalEventTypes = 0;
            int totalSubscribers = 0;

            // 4. 遍历绘制每个事件类型及其订阅者
            foreach (var kvp in listenerMaps)
            {
                Type eventType = kvp.Key;
                IList listeners = kvp.Value as IList; // 这里利用 IList 接口强转底层那个隐式的 List<IEventListener<T>>

                if (listeners == null || listeners.Count == 0) continue;

                // 过滤搜索
                if (!string.IsNullOrEmpty(_searchQuery) && eventType.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue; 
                }

                totalEventTypes++;
                totalSubscribers += listeners.Count;

                DrawEventCard(eventType, listeners);
            }

            if (totalEventTypes > 0)
            {
                EditorGUILayout.HelpBox($"当前频道活跃统计 | 事件种类: {totalEventTypes} | 监听器总数: {totalSubscribers}", MessageType.None);
            }
            else if (!string.IsNullOrEmpty(_searchQuery))
            {
                EditorGUILayout.HelpBox($"未搜索到包含 '{_searchQuery}' 的事件。", MessageType.Warning);
            }
        }

        private void DrawEventCard(Type eventType, IList listeners)
        {
            EditorGUILayout.BeginVertical("box");

            // --- 标题行（支持折叠） ---
            EditorGUILayout.BeginHorizontal();
            
            bool isExpanded = _expandedEventTypes.Contains(eventType);
            
            // 使用内建消息图标
            GUIContent icon = EditorGUIUtility.IconContent("Message");
            icon.text = $" {eventType.Name}";

            // 进度条背景色 (根据订阅数给点颜色看看，超过 5 个报警)
            var defaultColor = GUI.color;
            if (listeners.Count >= 10) GUI.color = new Color(1f, 0.4f, 0.4f);
            else if (listeners.Count >= 5) GUI.color = new Color(1f, 0.8f, 0.4f);
            else GUI.color = new Color(0.4f, 0.8f, 1f);

            bool nextExpanded = EditorGUILayout.Foldout(isExpanded, icon, true, EditorStyles.foldoutHeader);
            
            GUILayout.FlexibleSpace();
            
            // 绘制一个迷你进度条展示订阅数量热度
            Rect progressRect = GUILayoutUtility.GetRect(80, 16);
            EditorGUI.ProgressBar(progressRect, Mathf.Clamp01(listeners.Count / 15f), $"{listeners.Count} Subs");
            GUI.color = defaultColor;

            if (nextExpanded) _expandedEventTypes.Add(eventType);
            else _expandedEventTypes.Remove(eventType);

            EditorGUILayout.EndHorizontal();

            // --- 展开后的详细监听器列表 ---
            if (nextExpanded)
            {
                DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.2f));
                GUILayout.Space(5);

                for (int i = 0; i < listeners.Count; i++)
                {
                    object listenerObj = listeners[i];
                    if (listenerObj == null) continue;

                    DrawListenerRow(listenerObj, i);
                }
                GUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawListenerRow(object listenerObj, int index)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15); // 缩进

            Type listenerType = listenerObj.GetType();

            // 尝试获取 Priority 属性 (利用反射展示优先级)
            int priority = 0;
            var prop = listenerType.GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                priority = (int)prop.GetValue(listenerObj);
            }

            // 如果监听器是挂在 GameObject 上的 MonoBehaviour，我们可以让它变成可点击定位的按钮！
            if (listenerObj is MonoBehaviour mb && mb != null)
            {
                GUIContent mbIcon = EditorGUIUtility.IconContent("cs Script Icon");
                mbIcon.text = $" [{priority}] {mb.gameObject.name} ({listenerType.Name})";

                if (GUILayout.Button(mbIcon, EditorStyles.linkLabel, GUILayout.Height(20)))
                {
                    // 点击后自动在 Hierarchy 选中该 GameObject 并高亮！
                    Selection.activeGameObject = mb.gameObject;
                    EditorGUIUtility.PingObject(mb.gameObject);
                }
            }
            else
            {
                // 普通的纯 C# 类监听器
                GUIContent csIcon = EditorGUIUtility.IconContent("Assembly Icon");
                csIcon.text = $" [{priority}] {listenerType.Name} (C# Object)";
                GUILayout.Label(csIcon, GUILayout.Height(20));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLine(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        #endregion
    }
}