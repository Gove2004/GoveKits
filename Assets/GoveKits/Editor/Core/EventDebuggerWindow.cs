using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using GoveKits.Runtime.Core.Event;
using System;
using System.Linq;

/// <summary>
/// Event 系统调试窗口。
/// </summary>
/// <remarks>
/// 用于在 Play 模式下查看总线、频道订阅数和事件发布历史。
/// </remarks>
public class EventDebuggerWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private bool _showHistory = true;
    private string _selectedBus = "main";
    private string _eventTypeFilter = string.Empty;
    private bool _autoRefresh = true;
    private double _nextRepaintTime;
    private const double RepaintInterval = 0.25d;
    private GUIStyle _historyStyle;
    private readonly List<string> _cachedBusNames = new();
    private int _historyDisplayCount = 50;

    /// <summary>
    /// 打开调试窗口。
    /// </summary>
    [MenuItem("GoveKits/Core/Event Debugger")]
    public static void ShowWindow()
    {
        GetWindow<EventDebuggerWindow>("Event Debugger");
    }

    /// <summary>
    /// 订阅窗口刷新事件。
    /// </summary>
    private void OnEnable()
    {
        // 订阅刷新事件
        EventCore.OnEventSystemChanged += Repaint;
        EditorApplication.update += OnEditorUpdate;
        _nextRepaintTime = EditorApplication.timeSinceStartup;
    }

    /// <summary>
    /// 取消订阅窗口刷新事件。
    /// </summary>
    private void OnDisable()
    {
        EventCore.OnEventSystemChanged -= Repaint;
        EditorApplication.update -= OnEditorUpdate;
    }

    /// <summary>
    /// 编辑器更新回调，用于自动刷新窗口。
    /// </summary>
    private void OnEditorUpdate()
    {
        if (!_autoRefresh || !Application.isPlaying)
        {
            return;
        }

        if (EditorApplication.timeSinceStartup < _nextRepaintTime)
        {
            return;
        }

        _nextRepaintTime = EditorApplication.timeSinceStartup + RepaintInterval;
        Repaint();
    }

    private void EnsureStyles()
    {
        if (_historyStyle == null)
        {
            _historyStyle = new GUIStyle(EditorStyles.label);
            _historyStyle.normal.textColor = Color.cyan;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("GoveKits Event System Monitor", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();
        _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
        {
            Repaint();
        }

        if (GUILayout.Button("Clear History", EditorStyles.toolbarButton))
        {
            EventCore.EventHistory.Clear();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBusSelector()
    {
        _cachedBusNames.Clear();
        _cachedBusNames.AddRange(EventCore.GetDebugBusNames());
        if (_cachedBusNames.Count == 0)
        {
            _cachedBusNames.Add("main");
        }

        if (!_cachedBusNames.Contains(_selectedBus))
        {
            _selectedBus = _cachedBusNames[0];
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Bus", GUILayout.Width(50));
        int selectedIndex = Mathf.Max(0, _cachedBusNames.IndexOf(_selectedBus));
        int newIndex = EditorGUILayout.Popup(selectedIndex, _cachedBusNames.ToArray());
        _selectedBus = _cachedBusNames[Mathf.Clamp(newIndex, 0, _cachedBusNames.Count - 1)];
        EditorGUILayout.EndHorizontal();
    }

    private void DrawChannelPanel()
    {
        EditorGUILayout.Space(5);
        GUILayout.Label("Active Channels (Subscribers)", EditorStyles.whiteLargeLabel);

        _eventTypeFilter = EditorGUILayout.TextField("Event Filter", _eventTypeFilter);

        if (!EventCore.TryGetBus(_selectedBus, out var bus))
        {
            EditorGUILayout.HelpBox("Selected bus does not exist yet.", MessageType.Info);
            return;
        }

        var channels = bus.GetDebugChannels();
        var filteredChannels = channels
            .Where(kvp => string.IsNullOrWhiteSpace(_eventTypeFilter)
                || kvp.Key.Name.IndexOf(_eventTypeFilter, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key.Name)
            .ToList();

        int totalSubscribers = filteredChannels.Sum(c => c.Value);
        EditorGUILayout.HelpBox($"Bus: {_selectedBus} | Channels: {filteredChannels.Count} | Subscribers: {totalSubscribers}", MessageType.None);

        if (filteredChannels.Count == 0)
        {
            EditorGUILayout.HelpBox("No active subscribers in current view.", MessageType.Info);
            return;
        }

        foreach (var channel in filteredChannels)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"Type: {channel.Key.Name}", GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Subscribers: {channel.Value}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawHistoryPanel()
    {
        EditorGUILayout.Space(15);
        _showHistory = EditorGUILayout.BeginFoldoutHeaderGroup(_showHistory, "Publish History");
        if (_showHistory)
        {
            _historyDisplayCount = EditorGUILayout.IntSlider("Display Count", _historyDisplayCount, 10, 200);

            int shown = 0;
            int maxShow = Mathf.Min(_historyDisplayCount, EventCore.EventHistory.Count);
            for (int i = 0; i < maxShow; i++)
            {
                EditorGUILayout.LabelField(EventCore.EventHistory[i], _historyStyle);
                shown++;
            }

            if (shown == 0)
            {
                EditorGUILayout.HelpBox("No publish records yet.", MessageType.Info);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        DrawBusSelector();
        DrawChannelPanel();
        DrawHistoryPanel();

        EditorGUILayout.EndScrollView();
    }
}