using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoveKits.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    /// <summary>
    /// EventBus 内存全景监控窗口。
    /// 反射读取 EventCore 内部的订阅者映射表，实时展示各事件类型的监听器数量和详情。
    /// </summary>
    public class EventWindow : GoveKitsEditorWindow
    {
        /// <summary>通过反射获取 EventBus 内部 _listenerMaps 字段的缓存引用。</summary>
        private FieldInfo _listenerMapsField;

        /// <summary>通过反射获取 EventCore 内部 bus 字段的缓存引用。</summary>
        private FieldInfo _eventCoreBusField;

        /// <summary>已展开的事件类型集合，用于维护折叠面板状态。</summary>
        private readonly HashSet<Type> _expandedEventTypes = new();

        /// <summary>
        /// 显示 EventBus 监控窗口。菜单路径: GoveKits/Event。
        /// </summary>
        [MenuItem("GoveKits/Event", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<EventWindow>("Event 监控");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        protected override void OnGoveWindowEnable()
        {
            _listenerMapsField = typeof(EventBus).GetField("_listenerMaps", BindingFlags.NonPublic | BindingFlags.Static);
            _eventCoreBusField = typeof(EventCore).GetField("bus", BindingFlags.NonPublic | BindingFlags.Static);
            EnableAutoRefresh();
        }

        protected override void OnGoveDrawContent()
        {
            if (!Application.isPlaying)
            {
                GUILayout.Space(20);
                EditorGUILayout.HelpBox("事件总线监控需要在 Play 模式 (运行状态) 下才能抓取实时内存数据。", MessageType.Info);
                return;
            }

            DrawActiveBusData();
        }

        protected override void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("EventBus 内存全景监控", EditorStyles.largeLabel);

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("清空当前 Bus", GUILayout.Width(130), GUILayout.Height(24)))
            {
                ClearCurrentBus();
                Repaint();
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        protected override void DrawSearchToolbar()
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
            if (_eventCoreBusField == null)
            {
                EditorGUILayout.HelpBox("无法反射获取 EventBus 实例。", MessageType.Error);
                return;
            }

            var currentBus = _eventCoreBusField.GetValue(null) as EventBus;
            if (currentBus == null)
            {
                EditorGUILayout.HelpBox("EventBus 实例为空。", MessageType.Warning);
                return;
            }

            if (_listenerMapsField == null) return;
            var listenerMaps = _listenerMapsField.GetValue(currentBus) as IDictionary;
            if (listenerMaps == null)
            {
                EditorGUILayout.HelpBox("当前 EventBus 没有任何事件订阅。", MessageType.Info);
                return;
            }

            int totalEventTypes = 0;
            int totalSubscribers = 0;

            foreach (DictionaryEntry kvp in listenerMaps)
            {
                Type eventType = kvp.Key as Type;
                if (eventType == null) continue;

                IList listeners = kvp.Value as IList;
                if (listeners == null || listeners.Count == 0) continue;

                if (!string.IsNullOrEmpty(_searchQuery) && eventType.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                totalEventTypes++;
                totalSubscribers += listeners.Count;
                DrawEventCard(eventType, listeners);
            }

            if (totalEventTypes > 0)
            {
                EditorGUILayout.HelpBox($"当前活跃统计 | 事件种类: {totalEventTypes} | 监听器总数: {totalSubscribers}", MessageType.None);
            }
            else if (!string.IsNullOrEmpty(_searchQuery))
            {
                EditorGUILayout.HelpBox($"未搜索到包含 '{_searchQuery}' 的事件。", MessageType.Warning);
            }
        }

        private void DrawEventCard(Type eventType, IList listeners)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            bool isExpanded = _expandedEventTypes.Contains(eventType);

            GUIContent icon = EditorGUIUtility.IconContent("Message");
            icon.text = $" {eventType.Name}";

            var defaultColor = GUI.color;
            if (listeners.Count >= 10) GUI.color = new Color(1f, 0.4f, 0.4f);
            else if (listeners.Count >= 5) GUI.color = new Color(1f, 0.8f, 0.4f);
            else GUI.color = new Color(0.4f, 0.8f, 1f);

            bool nextExpanded = EditorGUILayout.Foldout(isExpanded, icon, true, EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            Rect progressRect = GUILayoutUtility.GetRect(80, 16);
            EditorGUI.ProgressBar(progressRect, Mathf.Clamp01(listeners.Count / 15f), $"{listeners.Count} Subs");
            GUI.color = defaultColor;

            if (nextExpanded) _expandedEventTypes.Add(eventType);
            else _expandedEventTypes.Remove(eventType);

            EditorGUILayout.EndHorizontal();

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
            GUILayout.Space(15);

            Type listenerType = listenerObj.GetType();

            int priority = 0;
            var prop = listenerType.GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                priority = (int)prop.GetValue(listenerObj);
            }

            if (listenerObj is MonoBehaviour mb && mb != null)
            {
                GUIContent mbIcon = EditorGUIUtility.IconContent("cs Script Icon");
                mbIcon.text = $" [{priority}] {mb.gameObject.name} ({listenerType.Name})";

                if (GUILayout.Button(mbIcon, EditorStyles.linkLabel, GUILayout.Height(20)))
                {
                    Selection.activeGameObject = mb.gameObject;
                    EditorGUIUtility.PingObject(mb.gameObject);
                }
            }
            else
            {
                GUIContent csIcon = EditorGUIUtility.IconContent("Assembly Icon");
                csIcon.text = $" [{priority}] {listenerType.Name} (C# Object)";
                GUILayout.Label(csIcon, GUILayout.Height(20));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ClearCurrentBus()
        {
            if (_eventCoreBusField == null) return;
            var currentBus = _eventCoreBusField.GetValue(null) as EventBus;
            if (currentBus == null) return;

            if (_listenerMapsField == null) return;
            var listenerMaps = _listenerMapsField.GetValue(currentBus) as Dictionary<Type, object>;
            if (listenerMaps == null) return;

            listenerMaps.Clear();

            var dirtyTypesField = typeof(EventBus).GetField("_dirtyTypes", BindingFlags.NonPublic | BindingFlags.Static);
            var dirtyTypes = dirtyTypesField?.GetValue(currentBus) as HashSet<Type>;
            dirtyTypes?.Clear();

            Debug.Log("<color=green>EventBus 监听器已全部清空。</color>");
        }

        #endregion
    }
}
