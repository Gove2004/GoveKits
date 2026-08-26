using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoveKits.Runtime.Util;
using GoveKits.Runtime.Storage;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    /// <summary>
    /// Localization 多语言调试窗口。
    /// 显示当前语言、翻译键列表、缺失翻译检测和快速修复功能。
    /// 通过反射读取 LocalizationCore 内部的翻译数据。
    /// </summary>
    public class LocalizationWindow : GoveKitsEditorWindow
    {
        private FieldInfo _rawRowsField;
        private FieldInfo _langCacheField;

        [MenuItem("GoveKits/Localization", false, 204)]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationWindow>("Localization 调试");
            window.minSize = new Vector2(550, 500);
            window.Show();
        }

        protected override void OnGoveWindowEnable()
        {
            _rawRowsField = typeof(LocalizationCore).GetField("_rawRows", BindingFlags.NonPublic | BindingFlags.Static);
            _langCacheField = typeof(LocalizationCore).GetField("_currentLangCache", BindingFlags.NonPublic | BindingFlags.Static);
        }

        private IDictionary _cachedRawRows;
        private IDictionary _cachedLangCache;

        protected override void OnGoveDrawContent()
        {
            if (_cachedRawRows == null || _cachedLangCache == null)
            {
                EditorGUILayout.HelpBox("LocalizationCore 尚未初始化或没有加载任何多语言数据。请先调用 LocalizationCore.Setup()。", MessageType.Warning);
                return;
            }

            DrawTranslationTable(_cachedRawRows, _cachedLangCache);
        }

        protected override void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Localization 多语言调试", EditorStyles.largeLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        protected override void DrawSearchToolbar()
        {
            // Re-read for stats display
            var rawRows = _rawRowsField?.GetValue(null) as IDictionary;
            var langCache = _langCacheField?.GetValue(null) as IDictionary;
            _cachedRawRows = rawRows;
            _cachedLangCache = langCache;

            int totalKeys = rawRows?.Count ?? 0;
            int filledKeys = langCache?.Count ?? 0;
            int missingKeys = totalKeys - filledKeys;

            // 语言选择
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("当前语言:", EditorStyles.boldLabel, GUILayout.Width(80));
            LanguageCode current = LocalizationCore.CurrentLanguage;
            LanguageCode newLang = (LanguageCode)EditorGUILayout.EnumPopup(current, GUILayout.Width(150));
            if (newLang != current)
            {
                LocalizationCore.SwitchLanguage(newLang);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            // 统计信息
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"总键数: {totalKeys}");
            EditorGUILayout.LabelField($"已翻译: {filledKeys}");
            var missingColor = missingKeys > 0 ? new Color(1f, 0.4f, 0.4f) : Color.green;
            var defaultColor = GUI.contentColor;
            GUI.contentColor = missingColor;
            EditorGUILayout.LabelField($"缺失: {missingKeys}");
            GUI.contentColor = defaultColor;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            // 搜索
            EditorGUILayout.BeginHorizontal("box");
            _searchQuery = EditorGUILayout.TextField("搜索:", _searchQuery, GUILayout.Height(20));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawTranslationTable(IDictionary rawRows, IDictionary langCache)
        {
            foreach (DictionaryEntry kvp in rawRows)
            {
                string key = kvp.Key as string;
                var rowData = kvp.Value;
                if (key == null || rowData == null) continue;

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    if (key.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                string currentText = langCache.Contains(key) ? (string)langCache[key] : "(缺失)";

                EditorGUILayout.BeginHorizontal("helpbox");

                EditorGUILayout.LabelField(key, EditorStyles.boldLabel, GUILayout.Width(150));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(LocalizationCore.CurrentLanguage.ToString() + ":", EditorStyles.miniLabel);
                GUI.skin.label.wordWrap = true;
                EditorGUILayout.TextArea(currentText, EditorStyles.wordWrappedMiniLabel, GUILayout.Height(40));
                GUI.skin.label.wordWrap = false;
                EditorGUILayout.EndVertical();

                if (currentText == "(缺失)")
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(150));
                    EditorGUILayout.LabelField("快速修复:", EditorStyles.miniLabel);
                    string fallback = GetFallbackText(rowData as ILocalizationConfigData);
                    if (!string.IsNullOrEmpty(fallback))
                    {
                        if (GUILayout.Button($"使用英文: {fallback.Substring(0, Mathf.Min(30, fallback.Length))}...", EditorStyles.miniButton))
                        {
                            SetLanguageField(rowData as ILocalizationConfigData, LocalizationCore.CurrentLanguage.ToString(), fallback);
                            Repaint();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }

        private string GetFallbackText(ILocalizationConfigData row)
        {
            if (row == null) return null;
            var englishField = row.GetType().GetField(LanguageCode.EnglishUS.ToString());
            return englishField?.GetValue(row) as string;
        }

        private void SetLanguageField(ILocalizationConfigData row, string fieldName, string value)
        {
            if (row == null) return;
            var field = row.GetType().GetField(fieldName);
            if (field != null)
            {
                field.SetValue(row, value);
            }
        }
    }
}
