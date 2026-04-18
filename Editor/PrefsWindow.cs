using System;
using System.Collections.Generic;
using GoveKits.Runtime.Storage;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    public class PrefsWindow : EditorWindow
    {
        private enum PrefsValueType
        {
            Int,
            Float,
            String,
            Bool,
        }

        private const string EditorKnownKeys = "GoveKits_Prefs_KnownKeys";
        private const char KeySeparator = '\n';

        private readonly List<string> _knownKeys = new();
        private Vector2 _knownKeysScroll;
        
        private string _key = string.Empty;
        private PrefsValueType _valueType = PrefsValueType.String;
        
        private int _intValue;
        private float _floatValue;
        private string _stringValue = string.Empty;
        private bool _boolValue;

        private string _status = "就绪";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("GoveKits/Prefs", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefsWindow>("Prefs 管理器");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadKnownKeys();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawEditorSection();
            DrawKnownKeysSection();
            DrawStatusBar();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("PlayerPrefs 键值对管理工具", EditorStyles.largeLabel);
            
            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("Delete All", GUILayout.Width(80), GUILayout.Height(24)))
            {
                TryDeleteAll();
            }
            GUI.backgroundColor = defaultColor;
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
            GUILayout.Space(10);
        }

        private void DrawEditorSection()
        {
            EditorGUILayout.LabelField("键值编辑器 (Key Editor)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");

            EditorGUIUtility.labelWidth = 60;
            _key = EditorGUILayout.TextField("Key", _key);
            _valueType = (PrefsValueType)EditorGUILayout.EnumPopup("Type", _valueType);

            switch (_valueType)
            {
                case PrefsValueType.Int:
                    _intValue = EditorGUILayout.IntField("Value", _intValue);
                    break;
                case PrefsValueType.Float:
                    _floatValue = EditorGUILayout.FloatField("Value", _floatValue);
                    break;
                case PrefsValueType.String:
                    _stringValue = EditorGUILayout.TextField("Value", _stringValue);
                    break;
                case PrefsValueType.Bool:
                    _boolValue = EditorGUILayout.Toggle("Value", _boolValue);
                    break;
            }
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(10);

            // 操作按钮排版
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set (设置)", GUILayout.Height(26))) TrySet();
            if (GUILayout.Button("Get (读取)", GUILayout.Height(26))) TryGet();
            if (GUILayout.Button("Check (检查)", GUILayout.Height(26))) TryHasKey();
            if (GUILayout.Button("Delete (删除)", GUILayout.Height(26))) TryDeleteKey();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("添加到常用列表 (Add To Known Keys)"))
            {
                AddKnownKey(_key);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawKnownKeysSection()
        {
            EditorGUILayout.LabelField("常用键列表 (Known Keys)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");

            if (_knownKeys.Count == 0)
            {
                EditorGUILayout.HelpBox("列表为空。在上方输入 Key 并点击添加即可保存到此列表。", MessageType.Info);
            }
            else
            {
                _knownKeysScroll = EditorGUILayout.BeginScrollView(_knownKeysScroll);
                for (int i = 0; i < _knownKeys.Count; i++)
                {
                    string item = _knownKeys[i];
                    EditorGUILayout.BeginHorizontal();

                    // 使用 Link 风格按钮，点击快速填充 Key
                    if (GUILayout.Button(item, EditorStyles.linkLabel))
                    {
                        _key = item;
                    }

                    var defaultColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                    {
                        RemoveKnownKey(item);
                        i--;
                    }
                    GUI.backgroundColor = defaultColor;

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusBar()
        {
            GUILayout.FlexibleSpace();
            DrawLine();
            EditorGUILayout.HelpBox(_status, _statusType);
        }

        #region 核心逻辑

        private void TrySet()
        {
            if (!ValidateKey()) return;

            switch (_valueType)
            {
                case PrefsValueType.Int: PrefsCore.SetInt(_key, _intValue); break;
                case PrefsValueType.Float: PrefsCore.SetFloat(_key, _floatValue); break;
                case PrefsValueType.String: PrefsCore.SetString(_key, _stringValue); break;
                case PrefsValueType.Bool: PrefsCore.SetBool(_key, _boolValue); break;
            }

            PrefsCore.Save();
            AddKnownKey(_key);
            SetStatus($"设置成功: {_key}", MessageType.Info);
        }

        private void TryGet()
        {
            if (!ValidateKey()) return;

            switch (_valueType)
            {
                case PrefsValueType.Int:
                    _intValue = PrefsCore.GetInt(_key);
                    SetStatus($"读取 Int: {_intValue}", MessageType.Info);
                    break;
                case PrefsValueType.Float:
                    _floatValue = PrefsCore.GetFloat(_key);
                    SetStatus($"读取 Float: {_floatValue}", MessageType.Info);
                    break;
                case PrefsValueType.String:
                    _stringValue = PrefsCore.GetString(_key);
                    SetStatus($"读取 String: {_stringValue}", MessageType.Info);
                    break;
                case PrefsValueType.Bool:
                    _boolValue = PrefsCore.GetBool(_key);
                    SetStatus($"读取 Bool: {_boolValue}", MessageType.Info);
                    break;
            }
        }

        private void TryHasKey()
        {
            if (!ValidateKey()) return;
            bool exists = PrefsCore.HasKey(_key);
            SetStatus(exists ? $"存在 Key: {_key}" : $"未找到 Key: {_key}", exists ? MessageType.Info : MessageType.Warning);
        }

        private void TryDeleteKey()
        {
            if (!ValidateKey()) return;
            PrefsCore.DeleteKey(_key);
            PrefsCore.Save();
            SetStatus($"已删除: {_key}", MessageType.Warning);
        }

        private void TryDeleteAll()
        {
            if (EditorUtility.DisplayDialog("警告", "确定要清除所有的 PlayerPrefs 数据吗？此操作无法撤销！", "确定清除", "取消"))
            {
                PrefsCore.DeleteAll();
                PrefsCore.Save();
                SetStatus("已清除所有 PlayerPrefs 数据。", MessageType.Error);
            }
        }

        private bool ValidateKey()
        {
            if (!string.IsNullOrWhiteSpace(_key)) return true;
            SetStatus("Key 不能为空！", MessageType.Error);
            return false;
        }

        private void SetStatus(string msg, MessageType type)
        {
            _status = msg;
            _statusType = type;
        }

        #endregion

        #region 缓存列表管理

        private void AddKnownKey(string newKey)
        {
            if (string.IsNullOrWhiteSpace(newKey) || _knownKeys.Contains(newKey)) return;
            _knownKeys.Add(newKey);
            _knownKeys.Sort(StringComparer.OrdinalIgnoreCase);
            SaveKnownKeys();
        }

        private void RemoveKnownKey(string removeKey)
        {
            if (_knownKeys.Remove(removeKey)) SaveKnownKeys();
        }

        private void LoadKnownKeys()
        {
            _knownKeys.Clear();
            string raw = EditorPrefs.GetString(EditorKnownKeys, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;

            string[] split = raw.Split(KeySeparator);
            foreach (string item in split)
            {
                if (!string.IsNullOrWhiteSpace(item) && !_knownKeys.Contains(item))
                    _knownKeys.Add(item);
            }
            _knownKeys.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private void SaveKnownKeys()
        {
            string raw = string.Join(KeySeparator.ToString(), _knownKeys);
            EditorPrefs.SetString(EditorKnownKeys, raw);
        }

        #endregion

        private void DrawLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
}