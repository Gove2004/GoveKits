// using System;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;
// using GoveKits.Runtime.Core;

// /// <summary>
// /// Pool 系统调试窗口。
// /// </summary>
// /// <remarks>
// /// 支持查看 C# 池与 GameObject 池运行时状态、容量阈值告警与历史记录。
// /// </remarks>
// public class PoolDebuggerWindow : EditorWindow
// {
//     private enum WarningLevel
//     {
//         None,
//         Warning,
//         Danger
//     }

//     private enum PoolView
//     {
//         CSharp,
//         GameObject
//     }

//     private Vector2 _scrollPos;
//     private bool _showHistory = true;
//     private bool _autoRefresh = true;
//     private double _nextRepaintTime;
//     private const double RepaintInterval = 0.25d;
//     private PoolView _activeView = PoolView.CSharp;
//     private string _filter = string.Empty;
//     private int _historyDisplayCount = 50;
//     private GUIStyle _historyStyle;
//     private bool _showOnlyWarnings;
//     private float _cacheWarnThreshold = 0.80f;
//     private float _cacheDangerThreshold = 0.95f;
//     private float _activeWarnThreshold = 0.70f;
//     private float _activeDangerThreshold = 0.90f;

//     /// <summary>
//     /// 打开调试窗口。
//     /// </summary>
//     [MenuItem("GoveKits/Core/Pool Debugger")]
//     public static void ShowWindow()
//     {
//         GetWindow<PoolDebuggerWindow>("Pool Debugger");
//     }

//     /// <summary>
//     /// 订阅窗口刷新事件。
//     /// </summary>
//     private void OnEnable()
//     {
//         PoolCore.OnPoolSystemChanged += Repaint;
//         EditorApplication.update += OnEditorUpdate;
//         _nextRepaintTime = EditorApplication.timeSinceStartup;
//     }

//     /// <summary>
//     /// 取消订阅窗口刷新事件。
//     /// </summary>
//     private void OnDisable()
//     {
//         PoolCore.OnPoolSystemChanged -= Repaint;
//         EditorApplication.update -= OnEditorUpdate;
//     }

//     /// <summary>
//     /// 编辑器更新回调，用于自动刷新窗口。
//     /// </summary>
//     private void OnEditorUpdate()
//     {
//         if (!_autoRefresh || !Application.isPlaying)
//         {
//             return;
//         }

//         if (EditorApplication.timeSinceStartup < _nextRepaintTime)
//         {
//             return;
//         }

//         _nextRepaintTime = EditorApplication.timeSinceStartup + RepaintInterval;
//         Repaint();
//     }

//     private void EnsureStyles()
//     {
//         if (_historyStyle == null)
//         {
//             _historyStyle = new GUIStyle(EditorStyles.label);
//             _historyStyle.normal.textColor = Color.cyan;
//         }
//     }

//     private void DrawToolbar()
//     {
//         EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//         GUILayout.Label("GoveKits Pool System Monitor", EditorStyles.boldLabel);

//         GUILayout.FlexibleSpace();
//         _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

//         if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
//         {
//             Repaint();
//         }

//         if (GUILayout.Button("Clear History", EditorStyles.toolbarButton))
//         {
//             PoolCore.PoolHistory.Clear();
//             Repaint();
//         }

//         EditorGUILayout.EndHorizontal();
//     }

//     private void DrawSummaryHeader(int pools, int cached, int active)
//     {
//         string activeText = _activeView == PoolView.GameObject ? $" | Active: {active}" : string.Empty;
//         EditorGUILayout.HelpBox($"View: {_activeView} | Pools: {pools} | Cached: {cached}{activeText}", MessageType.None);
//     }

//     private static WarningLevel EvaluateRatio(float ratio, float warnThreshold, float dangerThreshold)
//     {
//         if (ratio >= dangerThreshold)
//         {
//             return WarningLevel.Danger;
//         }

//         if (ratio >= warnThreshold)
//         {
//             return WarningLevel.Warning;
//         }

//         return WarningLevel.None;
//     }

//     private static float SafeRatio(int value, int total)
//     {
//         if (total <= 0)
//         {
//             return 0f;
//         }

//         return Mathf.Clamp01(value / (float)total);
//     }

//     private void DrawWarningSettings()
//     {
//         EditorGUILayout.Space(6);
//         GUILayout.Label("Warning Settings", EditorStyles.boldLabel);

//         _showOnlyWarnings = EditorGUILayout.ToggleLeft("Show Only Warning/Danger", _showOnlyWarnings);

//         _cacheWarnThreshold = EditorGUILayout.Slider("Cache Warn", _cacheWarnThreshold, 0.50f, 0.99f);
//         _cacheDangerThreshold = EditorGUILayout.Slider("Cache Danger", _cacheDangerThreshold, 0.50f, 0.99f);
//         _cacheDangerThreshold = Mathf.Max(_cacheWarnThreshold + 0.01f, _cacheDangerThreshold);

//         if (_activeView == PoolView.GameObject)
//         {
//             _activeWarnThreshold = EditorGUILayout.Slider("Active Warn", _activeWarnThreshold, 0.30f, 0.99f);
//             _activeDangerThreshold = EditorGUILayout.Slider("Active Danger", _activeDangerThreshold, 0.30f, 0.99f);
//             _activeDangerThreshold = Mathf.Max(_activeWarnThreshold + 0.01f, _activeDangerThreshold);
//         }
//     }

//     private void DrawCSharpPools()
//     {
//         var allPools = PoolCore.GetDebugCSharpPools()
//             .Where(p => string.IsNullOrWhiteSpace(_filter)
//                 || p.TypeName.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
//             .ToList();

//         int warningCount = 0;
//         int dangerCount = 0;

//         var pools = allPools
//             .Where(p =>
//             {
//                 float ratio = SafeRatio(p.CachedCount, p.MaxSize);
//                 WarningLevel level = EvaluateRatio(ratio, _cacheWarnThreshold, _cacheDangerThreshold);
//                 if (level == WarningLevel.Warning) warningCount++;
//                 if (level == WarningLevel.Danger) dangerCount++;
//                 return !_showOnlyWarnings || level != WarningLevel.None;
//             })
//             .ToList();

//         DrawSummaryHeader(pools.Count, pools.Sum(p => p.CachedCount), 0);
//         if (warningCount > 0 || dangerCount > 0)
//         {
//             EditorGUILayout.HelpBox($"Alerts -> Warning: {warningCount} | Danger: {dangerCount}", dangerCount > 0 ? MessageType.Error : MessageType.Warning);
//         }

//         if (pools.Count == 0)
//         {
//             EditorGUILayout.HelpBox("No C# pools in current view.", MessageType.Info);
//             return;
//         }

//         foreach (var pool in pools)
//         {
//             float ratio = SafeRatio(pool.CachedCount, pool.MaxSize);
//             WarningLevel level = EvaluateRatio(ratio, _cacheWarnThreshold, _cacheDangerThreshold);

//             EditorGUILayout.BeginHorizontal(GUI.skin.box);
//             GUILayout.Label($"Type: {pool.TypeName}", GUILayout.Width(200));
//             GUILayout.FlexibleSpace();
//             GUILayout.Label($"Cached: {pool.CachedCount}", GUILayout.Width(100));
//             GUILayout.Label($"Max: {pool.MaxSize}", EditorStyles.boldLabel, GUILayout.Width(80));

//             if (level == WarningLevel.Warning)
//             {
//                 GUILayout.Label("WARN", EditorStyles.miniBoldLabel, GUILayout.Width(45));
//             }
//             else if (level == WarningLevel.Danger)
//             {
//                 Color oldColor = GUI.contentColor;
//                 GUI.contentColor = Color.red;
//                 GUILayout.Label("DANGER", EditorStyles.miniBoldLabel, GUILayout.Width(55));
//                 GUI.contentColor = oldColor;
//             }
//             EditorGUILayout.EndHorizontal();
//         }
//     }

//     private void DrawGameObjectPools()
//     {
//         var allPools = PoolCore.GetDebugGameObjectPools()
//             .Where(p => string.IsNullOrWhiteSpace(_filter)
//                 || p.PrefabName.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
//             .ToList();

//         int warningCount = 0;
//         int dangerCount = 0;

//         var pools = allPools
//             .Where(p =>
//             {
//                 WarningLevel cacheLevel = EvaluateRatio(SafeRatio(p.CachedCount, p.MaxSize), _cacheWarnThreshold, _cacheDangerThreshold);
//                 WarningLevel activeLevel = EvaluateRatio(SafeRatio(p.ActiveCount, p.AllCount), _activeWarnThreshold, _activeDangerThreshold);
//                 WarningLevel level = (WarningLevel)Mathf.Max((int)cacheLevel, (int)activeLevel);
//                 if (level == WarningLevel.Warning) warningCount++;
//                 if (level == WarningLevel.Danger) dangerCount++;
//                 return !_showOnlyWarnings || level != WarningLevel.None;
//             })
//             .ToList();

//         DrawSummaryHeader(
//             pools.Count,
//             pools.Sum(p => p.CachedCount),
//             pools.Sum(p => p.ActiveCount));

//         if (warningCount > 0 || dangerCount > 0)
//         {
//             EditorGUILayout.HelpBox($"Alerts -> Warning: {warningCount} | Danger: {dangerCount}", dangerCount > 0 ? MessageType.Error : MessageType.Warning);
//         }

//         if (pools.Count == 0)
//         {
//             EditorGUILayout.HelpBox("No GameObject pools in current view.", MessageType.Info);
//             return;
//         }

//         foreach (var pool in pools)
//         {
//             WarningLevel cacheLevel = EvaluateRatio(SafeRatio(pool.CachedCount, pool.MaxSize), _cacheWarnThreshold, _cacheDangerThreshold);
//             WarningLevel activeLevel = EvaluateRatio(SafeRatio(pool.ActiveCount, pool.AllCount), _activeWarnThreshold, _activeDangerThreshold);
//             WarningLevel level = (WarningLevel)Mathf.Max((int)cacheLevel, (int)activeLevel);

//             EditorGUILayout.BeginVertical(GUI.skin.box);
//             EditorGUILayout.BeginHorizontal();
//             GUILayout.Label($"Prefab: {pool.PrefabName}", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();
//             GUILayout.Label($"ID: {pool.PrefabId}", GUILayout.Width(90));

//             if (level == WarningLevel.Warning)
//             {
//                 GUILayout.Label("WARN", EditorStyles.miniBoldLabel, GUILayout.Width(45));
//             }
//             else if (level == WarningLevel.Danger)
//             {
//                 Color oldColor = GUI.contentColor;
//                 GUI.contentColor = Color.red;
//                 GUILayout.Label("DANGER", EditorStyles.miniBoldLabel, GUILayout.Width(55));
//                 GUI.contentColor = oldColor;
//             }
//             EditorGUILayout.EndHorizontal();

//             EditorGUILayout.BeginHorizontal();
//             GUILayout.Label($"Cached: {pool.CachedCount}", GUILayout.Width(110));
//             GUILayout.Label($"Active: {pool.ActiveCount}", GUILayout.Width(110));
//             GUILayout.Label($"All: {pool.AllCount}", GUILayout.Width(110));
//             GUILayout.Label($"Max: {pool.MaxSize}", GUILayout.Width(90));
//             EditorGUILayout.EndHorizontal();
//             EditorGUILayout.EndVertical();
//         }
//     }

//     private void DrawHistory()
//     {
//         EditorGUILayout.Space(12);
//         _showHistory = EditorGUILayout.BeginFoldoutHeaderGroup(_showHistory, "Pool History");
//         if (_showHistory)
//         {
//             _historyDisplayCount = EditorGUILayout.IntSlider("Display Count", _historyDisplayCount, 10, 200);
//             int maxShow = Mathf.Min(_historyDisplayCount, PoolCore.PoolHistory.Count);

//             if (maxShow == 0)
//             {
//                 EditorGUILayout.HelpBox("No pool operations yet.", MessageType.Info);
//             }
//             else
//             {
//                 for (int i = 0; i < maxShow; i++)
//                 {
//                     EditorGUILayout.LabelField(PoolCore.PoolHistory[i], _historyStyle);
//                 }
//             }
//         }
//         EditorGUILayout.EndFoldoutHeaderGroup();
//     }

//     private void OnGUI()
//     {
//         EnsureStyles();
//         DrawToolbar();

//         _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

//         _activeView = (PoolView)GUILayout.Toolbar((int)_activeView, new[] { "C# Pools", "GameObject Pools" });
//         _filter = EditorGUILayout.TextField("Filter", _filter);
//         DrawWarningSettings();

//         EditorGUILayout.Space(6);

//         if (_activeView == PoolView.CSharp)
//         {
//             DrawCSharpPools();
//         }
//         else
//         {
//             DrawGameObjectPools();
//         }

//         DrawHistory();

//         EditorGUILayout.EndScrollView();
//     }
// }
