#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GoveKits.Runtime.Unit;

namespace GoveKits.Unit.Editor
{
    /// <summary>
    /// UnitBehaviour 及其子类的运行时监控 Inspector。
    /// </summary>
    /// <remarks>
    /// 在 Play 模式下展示 Attributes、Marks、Abilities、Reactions 的实时状态。
    /// </remarks>
    [CustomEditor(typeof(UnitBehaviour), true)] // true 表示支持所有继承自 UnitBehaviour 的子类
    public class UnitBehaviourEditor : UnityEditor.Editor
    {
        private UnitBehaviour _target;
        private string _filter = "";
        
        // 折叠栏状态
        private static bool _showAttributes = true;
        private static bool _showMarks = true;
        private static bool _showAbilities = true;
        private static bool _showReactions = false;

        private void OnEnable()
        {
            _target = (UnitBehaviour)target;
        }

        /// <summary>
        /// 开启实时刷新，让 Inspector 数值动态更新。
        /// </summary>
        /// <returns>始终返回 true。</returns>
        public override bool RequiresConstantRepaint() => true;

        /// <summary>
        /// 绘制自定义 Inspector。
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 绘制默认的脚本引用框
            base.DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("详细调试数据仅在运行时可见。", MessageType.Info);
                return;
            }

            if (_target.Attributes == null) return;

            EditorGUILayout.Space();
            DrawFilter();
            EditorGUILayout.Space();

            DrawAttributes();
            DrawMarks();
            DrawAbilities();
            DrawReactions();
        }

        private void DrawFilter()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🔍 过滤器", GUILayout.Width(60));
            _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _filter = "";
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
        }

        private bool IsVisible(string name)
        {
            if (string.IsNullOrEmpty(_filter)) return true;
            return name.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // --- 1. 属性 (Attributes) ---
        private void DrawAttributes()
        {
            _showAttributes = EditorGUILayout.Foldout(_showAttributes, $"📊 Attributes ({_target.Attributes.Count})", true, EditorStyles.foldoutHeader);
            if (_showAttributes)
            {
                EditorGUI.indentLevel++;
                if (_target.Attributes.Count == 0) EditorGUILayout.LabelField("Empty");

                foreach (var kv in _target.Attributes)
                {
                    if (!IsVisible(kv.Key.ToString())) continue;

                    var attr = kv.Value;
                    
                    // 区分 State (计算型) 和 Runtime (资源型)
                    bool isRuntime = attr is RuntimeAttribute;
                    GUI.color = isRuntime ? new Color(0.7f, 1f, 0.7f) : Color.white;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kv.Key.ToString(), GUILayout.Width(140));
                    
                    // 右侧显示数值
                    string valStr = attr.Value.ToString("F1");
                    
                    // 如果是 Runtime 属性，显示进度条风格
                    if (isRuntime)
                    {
                        var rt = attr as RuntimeAttribute;
                        if (rt.MaxValue > 0)
                        {
                            Rect rect = EditorGUILayout.GetControlRect();
                            EditorGUI.ProgressBar(rect, rt.Ratio, $"{rt.Value:F0} / {rt.MaxValue:F0}");
                        }
                        else
                        {
                            EditorGUILayout.LabelField(valStr);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(valStr, EditorStyles.boldLabel);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    GUI.color = Color.white;
                }
                EditorGUI.indentLevel--;
            }
        }

        // --- 2. 状态 (Marks) ---
        private void DrawMarks()
        {
            _showMarks = EditorGUILayout.Foldout(_showMarks, $"🏷️ Marks ({_target.Marks.Count})", true, EditorStyles.foldoutHeader);
            if (_showMarks)
            {
                EditorGUI.indentLevel++;
                if (_target.Marks.Count == 0) EditorGUILayout.LabelField("Empty");

                foreach (var kv in _target.Marks)
                {
                    if (!IsVisible(kv.Key.ToString())) continue;

                    var mark = kv.Value;
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // 第一行：名字 + 层数
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(mark.Name.ToString(), EditorStyles.boldLabel);
                    if (mark.MaxStack > 1)
                    {
                        GUILayout.Label($"Stack: {mark.Stack}/{mark.MaxStack}", GUILayout.Width(80));
                    }
                    EditorGUILayout.EndHorizontal();

                    // 第二行：持续时间进度条
                    if (mark.Duration > 0)
                    {
                        Rect rect = EditorGUILayout.GetControlRect(false, 16);
                        float progress = mark.Progress;
                        string label = $"{mark.Duration:F1}s";
                        
                        // 根据剩余时间变色 (快结束变红)
                        Color barColor = progress < 0.2f ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.7f, 1f);
                        DrawColoredProgressBar(rect, progress, label, barColor);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Duration: Infinite");
                    }

                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
            }
        }

        // --- 3. 技能 (Abilities) ---
        private void DrawAbilities()
        {
            _showAbilities = EditorGUILayout.Foldout(_showAbilities, $"⚔️ Abilities ({_target.Abilities.Count})", true, EditorStyles.foldoutHeader);
            if (_showAbilities)
            {
                EditorGUI.indentLevel++;
                if (_target.Abilities.Count == 0) EditorGUILayout.LabelField("Empty");

                foreach (var kv in _target.Abilities)
                {
                    if (!IsVisible(kv.Key.ToString())) continue;

                    var ability = kv.Value;
                    string name = ability.Name.ToString();

                    // 检查冷却状态
                    bool isOnCD = _target.Marks.HasTag($"CD.{name}");
                    var cdMark = isOnCD ? _target.Marks.GetMark<UnitMark>($"CD.{name}") : null;
                    bool canExecute = !isOnCD || (cdMark != null && cdMark.Duration <= 0);

                    EditorGUILayout.BeginHorizontal();
                    
                    // 状态指示灯
                    var iconColor = canExecute ? Color.green : Color.gray;
                    var style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor = iconColor;
                    GUILayout.Label("●", style, GUILayout.Width(20));

                    EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(150));
                    
                    if (!canExecute && isOnCD && cdMark != null && cdMark.Duration > 0)
                    {
                        float cd = cdMark.RemainingTime;
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField($"CD: {cd:F1}s");
                        GUI.color = Color.white;
                    }
                    else if (canExecute)
                    {
                         EditorGUILayout.LabelField("Ready", EditorStyles.miniLabel);
                    }
                    else
                    {
                        GUI.color = Color.gray;
                        EditorGUILayout.LabelField("不可用");
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        // --- 4. 反应 (Reactions) ---
        private void DrawReactions()
        {
            _showReactions = EditorGUILayout.Foldout(_showReactions, $"⚡ Reactions ({_target.Reactions.Count})", true, EditorStyles.foldoutHeader);
            if (_showReactions)
            {
                EditorGUI.indentLevel++;
                if (_target.Reactions.Count == 0) EditorGUILayout.LabelField("Empty");

                foreach (var kv in _target.Reactions)
                {
                    if (!IsVisible(kv.Key.ToString())) continue;
                    
                    var reaction = kv.Value;
                    EditorGUILayout.LabelField(kv.Key.ToString(), reaction.GetType().Name);
                }
                EditorGUI.indentLevel--;
            }
        }

        // 辅助：绘制自定义颜色的进度条
        private void DrawColoredProgressBar(Rect rect, float progress, string label, Color color)
        {
            var oldColor = GUI.color;
            // 背景
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            
            // 前景
            GUI.color = color;
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            GUI.DrawTexture(fillRect, EditorGUIUtility.whiteTexture);
            
            // 边框和文字 (恢复颜色绘制文字)
            GUI.color = Color.white;
            EditorGUI.DropShadowLabel(rect, label);
            
            GUI.color = oldColor;
        }
    }
}
#endif
