using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GoveKits.Runtime.Storage.Save;

namespace GoveKits.Editor.Save
{
    /// <summary>
    /// PlayerPrefs 管理窗口。
    /// </summary>
    public class PrefsManagerWindow : EditorWindow
    {
        private enum ValueType
        {
            Int,
            Float,
            String,
            Bool,
        }

        private const string EditorKnownKeys = "GoveKits.PrefsManager.KnownKeys";
        private const char KeySeparator = '\n';

        private readonly List<string> knownKeys = new();

        private Vector2 knownKeysScroll;
        private string key = string.Empty;
        private ValueType valueType = ValueType.String;

        private int intValue;
        private float floatValue;
        private string stringValue = string.Empty;
        private bool boolValue;

        private string status = "Ready";

        [MenuItem("GoveKits/Storage/Prefs Manager")]
        public static void ShowWindow()
        {
            GetWindow<PrefsManagerWindow>("Prefs Manager");
        }

        private void OnEnable()
        {
            LoadKnownKeys();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6);
            DrawEditor();
            EditorGUILayout.Space(10);
            DrawKnownKeys();
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("PlayerPrefs Manager", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                PrefsCore.Save();
                status = "PlayerPrefs saved.";
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("Delete All", EditorStyles.toolbarButton))
            {
                TryDeleteAll();
            }

            GUI.color = oldColor;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditor()
        {
            GUILayout.Label("Key Editor", EditorStyles.boldLabel);

            key = EditorGUILayout.TextField("Key", key);
            valueType = (ValueType)EditorGUILayout.EnumPopup("Type", valueType);

            switch (valueType)
            {
                case ValueType.Int:
                    intValue = EditorGUILayout.IntField("Value", intValue);
                    break;
                case ValueType.Float:
                    floatValue = EditorGUILayout.FloatField("Value", floatValue);
                    break;
                case ValueType.String:
                    stringValue = EditorGUILayout.TextField("Value", stringValue);
                    break;
                case ValueType.Bool:
                    boolValue = EditorGUILayout.Toggle("Value", boolValue);
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set"))
            {
                TrySet();
            }

            if (GUILayout.Button("Get"))
            {
                TryGet();
            }

            if (GUILayout.Button("Has Key"))
            {
                TryHasKey();
            }

            if (GUILayout.Button("Delete"))
            {
                TryDeleteKey();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add To Known Keys"))
            {
                AddKnownKey(key);
            }
        }

        private void DrawKnownKeys()
        {
            GUILayout.Label("Known Keys", EditorStyles.boldLabel);

            knownKeysScroll = EditorGUILayout.BeginScrollView(knownKeysScroll, GUILayout.Height(220));
            for (int i = 0; i < knownKeys.Count; i++)
            {
                string item = knownKeys[i];
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(item, EditorStyles.linkLabel))
                {
                    key = item;
                }

                if (GUILayout.Button("X", GUILayout.Width(22f)))
                {
                    RemoveKnownKey(item);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void TrySet()
        {
            if (!ValidateKey())
            {
                return;
            }

            switch (valueType)
            {
                case ValueType.Int:
                    PrefsCore.SetInt(key, intValue);
                    break;
                case ValueType.Float:
                    PrefsCore.SetFloat(key, floatValue);
                    break;
                case ValueType.String:
                    PrefsCore.SetString(key, stringValue);
                    break;
                case ValueType.Bool:
                    PrefsCore.SetBool(key, boolValue);
                    break;
            }

            PrefsCore.Save();
            AddKnownKey(key);
            status = $"Set success: {key}";
        }

        private void TryGet()
        {
            if (!ValidateKey())
            {
                return;
            }

            switch (valueType)
            {
                case ValueType.Int:
                    intValue = PrefsCore.GetInt(key);
                    status = $"Get int: {intValue}";
                    break;
                case ValueType.Float:
                    floatValue = PrefsCore.GetFloat(key);
                    status = $"Get float: {floatValue}";
                    break;
                case ValueType.String:
                    stringValue = PrefsCore.GetString(key);
                    status = $"Get string: {stringValue}";
                    break;
                case ValueType.Bool:
                    boolValue = PrefsCore.GetBool(key);
                    status = $"Get bool: {boolValue}";
                    break;
            }
        }

        private void TryHasKey()
        {
            if (!ValidateKey())
            {
                return;
            }

            bool exists = PrefsCore.HasKey(key);
            status = exists ? $"Exists: {key}" : $"Not found: {key}";
        }

        private void TryDeleteKey()
        {
            if (!ValidateKey())
            {
                return;
            }

            PrefsCore.DeleteKey(key);
            PrefsCore.Save();
            status = $"Deleted: {key}";
        }

        private void TryDeleteAll()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete All PlayerPrefs",
                "Delete all PlayerPrefs data? This cannot be undone.",
                "Delete All",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            PrefsCore.DeleteAll();
            PrefsCore.Save();
            status = "Deleted all PlayerPrefs.";
        }

        private bool ValidateKey()
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                return true;
            }

            status = "Key is empty.";
            return false;
        }

        private void AddKnownKey(string newKey)
        {
            if (string.IsNullOrWhiteSpace(newKey))
            {
                return;
            }

            if (knownKeys.Contains(newKey))
            {
                return;
            }

            knownKeys.Add(newKey);
            knownKeys.Sort(StringComparer.OrdinalIgnoreCase);
            SaveKnownKeys();
        }

        private void RemoveKnownKey(string removeKey)
        {
            if (knownKeys.Remove(removeKey))
            {
                SaveKnownKeys();
            }
        }

        private void LoadKnownKeys()
        {
            knownKeys.Clear();
            string raw = EditorPrefs.GetString(EditorKnownKeys, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            string[] split = raw.Split(KeySeparator);
            for (int i = 0; i < split.Length; i++)
            {
                string item = split[i];
                if (!string.IsNullOrWhiteSpace(item) && !knownKeys.Contains(item))
                {
                    knownKeys.Add(item);
                }
            }

            knownKeys.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private void SaveKnownKeys()
        {
            string raw = string.Join(KeySeparator.ToString(), knownKeys);
            EditorPrefs.SetString(EditorKnownKeys, raw);
        }
    }
}
