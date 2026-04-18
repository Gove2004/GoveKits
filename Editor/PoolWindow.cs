using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoveKits.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class PoolWindow : EditorWindow
    {
        private enum PoolTab
        {
            CSharpPools,
            GameObjectPools
        }

        private Vector2 _scrollPos;
        private PoolTab _activeTab = PoolTab.CSharpPools;
        private string _searchQuery = string.Empty;
        
        private bool _autoRefresh = true;
        private double _nextRefreshTime;
        private const double RefreshInterval = 0.5;

        private FieldInfo _csharpPoolsField;
        private FieldInfo _goPoolsField;
        private FieldInfo _goPrefabField;

        [MenuItem("GoveKits/Pool", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<PoolWindow>("Pool 监控");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            _csharpPoolsField = typeof(PoolCore).GetField("_csharpPools", BindingFlags.NonPublic | BindingFlags.Static);
            _goPoolsField = typeof(PoolCore).GetField("_gameObjectPools", BindingFlags.NonPublic | BindingFlags.Static);
            _goPrefabField = typeof(GameObjectPool).GetField("_prefab", BindingFlags.NonPublic | BindingFlags.Instance);

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
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("需要在 Play 模式下才能查看实时数据。", MessageType.Info);
            }
            else
            {
                if (_activeTab == PoolTab.CSharpPools) DrawCSharpPools();
                else DrawGameObjectPools();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pool 实时监控", EditorStyles.boldLabel);

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("Clear All", GUILayout.Width(100), GUILayout.Height(20)))
            {
                PoolCore.Clear();
                Repaint();
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            _activeTab = (PoolTab)GUILayout.Toolbar((int)_activeTab, new[] { "C# 对象池", "GameObject 对象池" }, GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal("box");
            EditorGUIUtility.labelWidth = 40;
            _searchQuery = EditorGUILayout.TextField("搜索:", _searchQuery);
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(10);
            _autoRefresh = EditorGUILayout.ToggleLeft("自动刷新", _autoRefresh, GUILayout.Width(70));
            if (GUILayout.Button("刷新", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        #region 反射读取与绘制

        private void DrawCSharpPools()
        {
            if (_csharpPoolsField == null) return;
            var csharpPools = _csharpPoolsField.GetValue(null) as IDictionary;

            if (csharpPools == null || csharpPools.Count == 0) return;
            DrawSummaryHeader("C# Class", csharpPools.Count);

            foreach (DictionaryEntry kvp in csharpPools)
            {
                Type type = kvp.Key as Type;
                IPool pool = kvp.Value as IPool;

                if (type == null || pool == null) continue;
                string typeName = type.Name;
                
                if (!string.IsNullOrEmpty(_searchQuery) && typeName.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;
                DrawPoolCard(typeName, pool);
            }
        }

        private void DrawGameObjectPools()
        {
            if (_goPoolsField == null) return;
            var goPools = _goPoolsField.GetValue(null) as IDictionary;

            if (goPools == null || goPools.Count == 0) return;
            DrawSummaryHeader("GameObject", goPools.Count);

            foreach (DictionaryEntry kvp in goPools)
            {
                GameObjectPool pool = kvp.Value as GameObjectPool;
                if (pool == null) continue;

                GameObject prefab = _goPrefabField?.GetValue(pool) as GameObject;
                string prefabName = prefab != null ? prefab.name : "Unknown Prefab";

                if (!string.IsNullOrEmpty(_searchQuery) && prefabName.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;
                DrawPoolCard(prefabName, pool);
            }
        }

        private void DrawSummaryHeader(string type, int count)
        {
            GUILayout.Label($"已缓存的 {type} 池数量: {count}", EditorStyles.miniBoldLabel);
            GUILayout.Space(2);
        }

        private void DrawPoolCard(string name, IPool pool)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label(name, EditorStyles.boldLabel); // 去掉了图标，直接高亮显示名字
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                pool.Clear();
            }
            EditorGUILayout.EndHorizontal();

            float fillRatio = pool.MaxSize > 0 ? (float)pool.CachedCount / pool.MaxSize : 0;
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"库存: {pool.CachedCount} / {pool.MaxSize}", EditorStyles.miniLabel, GUILayout.Width(100));

            var defaultColor = GUI.color;
            if (fillRatio >= 0.95f) GUI.color = new Color(1f, 0.4f, 0.4f);
            else if (fillRatio >= 0.80f) GUI.color = new Color(1f, 0.8f, 0.4f);
            else GUI.color = new Color(0.4f, 0.8f, 1f);

            Rect progressRect = GUILayoutUtility.GetRect(100, 14, GUILayout.ExpandWidth(true)); // 高度压扁
            EditorGUI.ProgressBar(progressRect, fillRatio, $"{fillRatio * 100:F0}%");
            GUI.color = defaultColor;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void DrawLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        #endregion
    }
}