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
    /// TimeWheel 定时器监控窗口。
    /// 反射读取 TimeCore 内部的 TimeWheel，列出所有活跃定时器。
    /// </summary>
    public class TimeWindow : GoveKitsEditorWindow
    {
        /// <summary>反射获取 TimeWheel 实例的字段。</summary>
        private FieldInfo _wheelField;

        /// <summary>反射获取 TimeWheel 槽位数组的字段。</summary>
        private FieldInfo _slotsField;

        /// <summary>反射获取当前 Tick 值的字段。</summary>
        private FieldInfo _currentTickField;

        /// <summary>反射获取槽位大小的字段。</summary>
        private FieldInfo _wheelSizeField;

        /// <summary>反射获取 Tick 持续时间的字段。</summary>
        private FieldInfo _tickDurationField;

        /// <summary>反射获取定时器目标 Tick 的字段。</summary>
        private FieldInfo _timerTargetTickField;

        /// <summary>反射获取定时器间隔的字段。</summary>
        private FieldInfo _timerIntervalField;

        /// <summary>反射获取定时器循环次数的字段。</summary>
        private FieldInfo _timerLoopCountField;

        /// <summary>
        /// 显示 TimeWheel 监控窗口。
        /// </summary>
        [MenuItem("GoveKits/Time", false, 102)]
        public static void ShowWindow()
        {
            var window = GetWindow<TimeWindow>("TimeWheel 监控");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        protected override void OnGoveWindowEnable()
        {
            _wheelField = typeof(TimeCore).GetField("wheel", BindingFlags.NonPublic | BindingFlags.Static);
            _currentTickField = typeof(TimeWheel).GetField("_currentTick", BindingFlags.NonPublic | BindingFlags.Instance);
            _wheelSizeField = typeof(TimeWheel).GetField("_wheelSize", BindingFlags.NonPublic | BindingFlags.Instance);
            _tickDurationField = typeof(TimeWheel).GetField("_tickDuration", BindingFlags.NonPublic | BindingFlags.Instance);
            _slotsField = typeof(TimeWheel).GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            _timerTargetTickField = typeof(Timer).GetField("TargetTick", BindingFlags.NonPublic | BindingFlags.Instance);
            _timerIntervalField = typeof(Timer).GetField("Interval", BindingFlags.NonPublic | BindingFlags.Instance);
            _timerLoopCountField = typeof(Timer).GetField("LoopCount", BindingFlags.NonPublic | BindingFlags.Instance);
            EnableAutoRefresh();
        }

        protected override void OnGoveDrawContent()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要在 Play 模式下才能查看实时数据。", MessageType.Info);
                return;
            }

            if (_wheelField == null)
            {
                EditorGUILayout.HelpBox("无法反射获取 TimeWheel 实例。", MessageType.Error);
                return;
            }

            var wheel = _wheelField.GetValue(null);
            if (wheel == null)
            {
                EditorGUILayout.HelpBox("TimeWheel 尚未初始化。请先调用 TimeCore.Setup()。", MessageType.Warning);
                return;
            }

            long currentTick = (long)(_currentTickField?.GetValue(wheel) ?? 0);
            int wheelSize = (int)(_wheelSizeField?.GetValue(wheel) ?? 0);
            float tickDuration = (float)(_tickDurationField?.GetValue(wheel) ?? 0.05f);

            EditorGUILayout.LabelField($"当前Tick: {currentTick} | 槽位数: {wheelSize} | Tick间隔: {tickDuration:F3}s");
            GUILayout.Space(5);

            var slots = _slotsField?.GetValue(wheel) as ArrayList;
            if (slots != null)
            {
                int totalTimers = 0;
                int activeTimers = 0;
                int pausedTimers = 0;
                int cancelledTimers = 0;

                for (int i = 0; i < slots.Count; i++)
                {
                    var list = slots[i] as LinkedList<Timer>;
                    if (list == null) continue;

                    foreach (var timer in list)
                    {
                        totalTimers++;
                        if (timer.IsCancelled) cancelledTimers++;
                        else if (timer.IsPaused) pausedTimers++;
                        else activeTimers++;

                        if (!string.IsNullOrEmpty(_searchQuery))
                        {
                            var idStr = timer.Id.ToString();
                            if (idStr.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        }

                        DrawTimerRow(timer);
                    }
                }

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"总计: {totalTimers} | 活跃: {activeTimers} | 暂停: {pausedTimers} | 已取消: {cancelledTimers}");
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTimerRow(Timer timer)
        {
            EditorGUILayout.BeginHorizontal("helpbox");

            var statusColor = Color.green;
            if (timer.IsCancelled) statusColor = Color.red;
            else if (timer.IsPaused) statusColor = Color.yellow;
            else if (timer.IsDone) statusColor = Color.gray;

            var defaultColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            EditorGUILayout.LabelField($"[{timer.Id}]", EditorStyles.boldLabel, GUILayout.Width(50));
            GUI.contentColor = defaultColor;

            EditorGUILayout.LabelField($"目标Tick: {(_timerTargetTickField?.GetValue(timer) ?? 0)}");
            EditorGUILayout.LabelField($"间隔: {(_timerIntervalField?.GetValue(timer) ?? 0f):F3}");
            EditorGUILayout.LabelField($"循环次数: {(_timerLoopCountField?.GetValue(timer) ?? 0)}");

            if (!timer.IsDone && !timer.IsCancelled)
            {
                if (timer.IsPaused)
                {
                    if (GUILayout.Button("恢复", GUILayout.Width(40))) timer.Resume();
                }
                else
                {
                    if (GUILayout.Button("暂停", GUILayout.Width(40))) timer.Pause();
                }
                if (GUILayout.Button("取消", GUILayout.Width(40))) timer.Cancel();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
