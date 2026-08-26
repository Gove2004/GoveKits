using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoveKits.Runtime.Util;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    /// <summary>
    /// SpawnCore 实体监控窗口。
    /// 反射读取 SpawnCore 内部的工厂注册表和存活实体列表。
    /// </summary>
    public class SpawnWindow : GoveKitsEditorWindow
    {
        /// <summary>当前激活的标签页。</summary>
        private SpawnTab _activeTab = SpawnTab.Entities;

        /// <summary>标签页枚举。</summary>
        private enum SpawnTab { Factories, Entities }

        /// <summary>反射获取工厂注册表的字段。</summary>
        private FieldInfo _factoriesField;

        /// <summary>反射获取存活实体列表的字段。</summary>
        private FieldInfo _entitiesField;

        /// <summary>反射获取消亡动作注册的字段。</summary>
        private FieldInfo _despawnActionsField;

        /// <summary>
        /// 显示 Spawn 监控窗口。
        /// </summary>
        [MenuItem("GoveKits/Spawn", false, 103)]
        public static void ShowWindow()
        {
            var window = GetWindow<SpawnWindow>("Spawn 监控");
            window.minSize = new Vector2(550, 400);
            window.Show();
        }

        protected override void OnGoveWindowEnable()
        {
            _factoriesField = typeof(SpawnCore).GetField("spawnFactories", BindingFlags.NonPublic | BindingFlags.Static);
            _despawnActionsField = typeof(SpawnCore).GetField("despawnActions", BindingFlags.NonPublic | BindingFlags.Static);
            _entitiesField = typeof(SpawnCore).GetField("spawnedEntities", BindingFlags.NonPublic | BindingFlags.Static);
            EnableAutoRefresh();
        }

        protected override void OnGoveDrawContent()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要在 Play 模式下才能查看实时数据。", MessageType.Info);
                return;
            }

            if (_activeTab == SpawnTab.Entities) DrawEntities();
            else DrawFactories();
        }

        protected override void DrawSearchToolbar()
        {
            EditorGUILayout.BeginHorizontal("box");
            _searchQuery = EditorGUILayout.TextField("搜索:", _searchQuery, GUILayout.Height(20));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawEntities()
        {
            if (_entitiesField == null)
            {
                EditorGUILayout.HelpBox("无法反射获取实体列表。", MessageType.Error);
                return;
            }

            var entities = _entitiesField.GetValue(null) as IDictionary;
            if (entities == null)
            {
                EditorGUILayout.HelpBox("当前没有存活实体。", MessageType.Info);
                return;
            }

            int count = 0;
            foreach (DictionaryEntry kvp in entities)
            {
                uint id = (uint)kvp.Key;
                ISpawnable entity = kvp.Value as ISpawnable;
                if (entity == null) continue;

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    var idStr = id.ToString();
                    var typeStr = entity.GetType().Name;
                    var keyStr = entity.SpawnKey;
                    if (idStr.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        typeStr.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                        keyStr.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                count++;
                EditorGUILayout.BeginHorizontal("helpbox");

                EditorGUILayout.LabelField($"ID: {id}", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField($"类型: {entity.GetType().Name}");
                EditorGUILayout.LabelField($"键: {entity.SpawnKey}");

                if (GUILayout.Button("销毁", GUILayout.Width(60)))
                {
                    SpawnCore.Despawn(id);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (count == 0)
                EditorGUILayout.HelpBox("未搜索到匹配的实体。", MessageType.Info);
            else
                EditorGUILayout.LabelField($"共 {count} 个存活实体", EditorStyles.miniLabel);
        }

        private void DrawFactories()
        {
            if (_factoriesField == null)
            {
                EditorGUILayout.HelpBox("无法反射获取工厂列表。", MessageType.Error);
                return;
            }

            var factories = _factoriesField.GetValue(null) as IDictionary;
            if (factories == null || factories.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有注册任何 Spawn 工厂。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"已注册 {factories.Count} 个工厂", EditorStyles.miniLabel);
            GUILayout.Space(5);

            foreach (DictionaryEntry kvp in factories)
            {
                string spawnKey = kvp.Key as string;
                var factory = kvp.Value;

                EditorGUILayout.BeginHorizontal("helpbox");
                EditorGUILayout.LabelField($"键: {spawnKey}", EditorStyles.boldLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField($"工厂: {factory?.GetType().Name ?? "null"}");
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
