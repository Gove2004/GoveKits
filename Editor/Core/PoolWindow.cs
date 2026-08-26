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
    /// Pool 实时监控窗口。
    /// 反射读取 PoolCore 内部的 C# 对象池和 GameObject 对象池数据，以可视化卡片形式展示容量和使用情况。
    /// </summary>
    public class PoolWindow : GoveKitsEditorWindow
    {
        /// <summary>对象池数据源的枚举类型。</summary>
        private enum PoolTab
        {
            CSharpPools,
            GameObjectPools
        }

        /// <summary>当前激活的对象池 Tab。</summary>
        private PoolTab _activeTab = PoolTab.CSharpPools;

        /// <summary>通过反射缓存 PoolCore 内部 csharpPools 字段的引用。</summary>
        private FieldInfo _csharpPoolsField;

        /// <summary>通过反射缓存 PoolCore 内部 gameObjectPools 字段的引用。</summary>
        private FieldInfo _goPoolsField;

        /// <summary>通过反射缓存 GameObjectPool 内部 prefab 字段的引用。</summary>
        private FieldInfo _goPrefabField;

        /// <summary>
        /// 显示 Pool 监控窗口。菜单路径: GoveKits/Pool。
        /// </summary>
        [MenuItem("GoveKits/Pool", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<PoolWindow>("Pool 监控");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        /// <summary>窗口启用时初始化反射字段缓存并注册编辑器更新回调。</summary>
        protected override void OnGoveWindowEnable()
        {
            _csharpPoolsField = typeof(PoolCore).GetField("csharpPools", BindingFlags.NonPublic | BindingFlags.Static);
            _goPoolsField = typeof(PoolCore).GetField("gameObjectPools", BindingFlags.NonPublic | BindingFlags.Static);
            _goPrefabField = typeof(GameObjectPool).GetField("prefab", BindingFlags.NonPublic | BindingFlags.Instance);
            EnableAutoRefresh();
        }

        protected override void OnGoveDrawContent()
        {
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
        }

        protected override void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pool 实时监控", EditorStyles.boldLabel);

            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("清空当前 Tab", GUILayout.Width(100), GUILayout.Height(20)))
            {
                ClearCurrentTab();
                Repaint();
            }
            GUI.backgroundColor = defaultColor;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        protected override void DrawSearchToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            _activeTab = (PoolTab)GUILayout.Toolbar((int)_activeTab, new[] { "C# 对象池", "GameObject 对象池" }, GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            base.DrawSearchToolbar();
        }

        #region 反射读取与绘制

        private void DrawCSharpPools()
        {
            if (_csharpPoolsField == null) return;

            var csharpPools = _csharpPoolsField.GetValue(null) as IDictionary;

            if (csharpPools == null || csharpPools.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有任何 C# 对象池。请先通过 PoolCore.Create<T>() 创建。", MessageType.Info);
                return;
            }

            GUILayout.Label($"已缓存的 C# Class 池数量: {csharpPools.Count}", EditorStyles.miniBoldLabel);
            GUILayout.Space(2);

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

            if (goPools == null || goPools.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有任何 GameObject 对象池。请先通过 PoolCore.Create(prefab) 创建。", MessageType.Info);
                return;
            }

            GUILayout.Label($"已缓存的 GameObject 池数量: {goPools.Count}", EditorStyles.miniBoldLabel);
            GUILayout.Space(2);

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

        private void DrawPoolCard(string name, IPool pool)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                pool.Clear();
            }
            EditorGUILayout.EndHorizontal();

            float fillRatio = pool.Capacity > 0 ? (float)pool.Count / pool.Capacity : 0;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"库存: {pool.Count} / {pool.Capacity}", EditorStyles.miniLabel, GUILayout.Width(100));

            var defaultColor = GUI.color;
            if (fillRatio >= 0.95f) GUI.color = new Color(1f, 0.4f, 0.4f);
            else if (fillRatio >= 0.80f) GUI.color = new Color(1f, 0.8f, 0.4f);
            else GUI.color = new Color(0.4f, 0.8f, 1f);

            Rect progressRect = GUILayoutUtility.GetRect(100, 14, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, fillRatio, $"{fillRatio * 100:F0}%");
            GUI.color = defaultColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void ClearCurrentTab()
        {
            IDictionary pools = null;
            if (_activeTab == PoolTab.CSharpPools)
            {
                pools = _csharpPoolsField?.GetValue(null) as IDictionary;
            }
            else
            {
                pools = _goPoolsField?.GetValue(null) as IDictionary;
            }

            if (pools == null) return;

            foreach (DictionaryEntry kvp in pools)
            {
                (kvp.Value as IPool)?.Clear();
            }
        }

        #endregion
    }
}
