#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GoveKits.Runtime.Unit;

namespace GoveKits.Editor.Unit
{
    /// <summary>
    /// UnitBehaviour 核心运行时监控 Inspector。
    /// </summary>
    /// <remarks>
    /// 在 Play 模式下，实时展现四大容器（属性、标记、技能、反应）的数据变化。
    /// 完全适配新版零 GC 和数据驱动架构。
    /// </remarks>
    [CustomEditor(typeof(UnitBehaviour), true)] 
    public class UnitWindow : UnityEditor.Editor
    {
        private UnitBehaviour _target;
        private string _searchFilter = "";
        
        // 折叠栏缓存状态
        private static bool _showAttributes = true;
        private static bool _showMarks = true;
        private static bool _showAbilities = true;
        private static bool _showReactions = false;

        private void OnEnable()
        {
            _target = (UnitBehaviour)target;
        }

        /// <summary>
        /// 开启实时刷新，让 Inspector 数值随游戏帧动态跳动。
        /// </summary>
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            // 绘制原本公开在 MonoBehaviour 里的配置字段（比如你的 maxHealth 等）
            base.DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("详细的 Unit 容器状态监控，仅在 Play 模式下可见。", MessageType.Info);
                return;
            }

            // 确保四大容器已经初始化
            if (_target.Attributes == null) return;

            EditorGUILayout.Space(10);
            DrawHeaderAndFilter();
            EditorGUILayout.Space(5);

            DrawAttributes();
            DrawMarks();
            DrawAbilities();
            DrawReactions();
            
            GUILayout.Space(10);
        }

        private void DrawHeaderAndFilter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("核心数据监控 (Live Data)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // 优雅的原生搜索条
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                _searchFilter = "";
                GUI.FocusControl(null); // 取消焦点，收起软键盘
            }
            EditorGUILayout.EndHorizontal();
            
            DrawLine(new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private bool IsMatchFilter(string name)
        {
            if (string.IsNullOrEmpty(_searchFilter)) return true;
            return name.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #region 容器绘制模块

        // --- 1. 属性 (Attributes) ---
        private void DrawAttributes()
        {
            _showAttributes = EditorGUILayout.Foldout(_showAttributes, $"📊 属性面板 - Attributes ({_target.Attributes.Count})", true, EditorStyles.foldoutHeader);
            if (!_showAttributes) return;

            EditorGUI.indentLevel++;
            if (_target.Attributes.Count == 0) EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);

            foreach (var kv in _target.Attributes)
            {
                if (!IsMatchFilter(kv.Key)) continue;

                var attr = kv.Value;
                
                EditorGUILayout.BeginHorizontal("helpbox");
                
                // 属性名
                EditorGUILayout.LabelField(kv.Key, GUILayout.Width(120));
                GUILayout.FlexibleSpace();

                // 核心：如果有 Modifier 介入，用颜色高亮区分并展示详细信息
                if (attr.Modifiers.Count > 0)
                {
                    GUI.contentColor = new Color(0.4f, 0.8f, 1f); // 浅蓝色提示有修改器
                    GUILayout.Label($"[Mods: {attr.Modifiers.Count}]", EditorStyles.miniLabel, GUILayout.Width(65));
                    
                    // 对比基础值与当前值
                    GUI.contentColor = Color.gray;
                    GUILayout.Label($"Base: {attr.BaseValue:F1} →", EditorStyles.miniLabel, GUILayout.Width(85));
                    
                    GUI.contentColor = attr.CurrentValue > attr.BaseValue ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.4f, 0.4f);
                    GUILayout.Label(attr.CurrentValue.ToString("F1"), EditorStyles.boldLabel, GUILayout.Width(50));
                }
                else
                {
                    // 纯净状态，无修改器
                    GUI.contentColor = Color.white;
                    GUILayout.Label(attr.CurrentValue.ToString("F1"), EditorStyles.boldLabel, GUILayout.Width(50));
                }
                
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        // --- 2. 状态 (Marks) ---
        private void DrawMarks()
        {
            _showMarks = EditorGUILayout.Foldout(_showMarks, $"🏷️ 标记与状态 - Marks ({_target.Marks.Count})", true, EditorStyles.foldoutHeader);
            if (!_showMarks) return;

            EditorGUI.indentLevel++;
            if (_target.Marks.Count == 0) EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);

            foreach (var kv in _target.Marks)
            {
                if (!IsMatchFilter(kv.Key)) continue;

                var mark = kv.Value;
                EditorGUILayout.BeginVertical("helpbox");
                
                // 标题行
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(mark.Name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                if (mark.MaxStack > 1)
                {
                    GUI.contentColor = new Color(1f, 0.8f, 0.4f);
                    GUILayout.Label($"[Stack: {mark.Stack}/{mark.MaxStack}]", EditorStyles.miniBoldLabel);
                    GUI.contentColor = Color.white;
                }
                
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    // 允许在编辑器一键移除某个状态进行调试
                    _target.Marks.RemoveMark(mark.Name);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue; 
                }
                EditorGUILayout.EndHorizontal();

                // 进度条行 (原生组件)
                if (mark.Duration > 0)
                {
                    // 计算剩余比例 (1 -> 0)
                    float remainingRatio = mark.RemainingTime / mark.Duration;
                    
                    var defaultColor = GUI.color;
                    if (remainingRatio < 0.25f) GUI.color = new Color(1f, 0.4f, 0.4f); // 快结束时变红
                    else GUI.color = new Color(0.4f, 0.8f, 1f); // 正常浅蓝色

                    Rect rect = GUILayoutUtility.GetRect(100, 16, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(rect, remainingRatio, $"{mark.RemainingTime:F1}s / {mark.Duration:F1}s");
                    GUI.color = defaultColor;
                }
                else
                {
                    GUILayout.Label("持续时间: 永久 (Infinite)", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }

        // --- 3. 技能 (Abilities) ---
        private void DrawAbilities()
        {
            _showAbilities = EditorGUILayout.Foldout(_showAbilities, $"⚔️ 技能容器 - Abilities ({_target.Abilities.Count})", true, EditorStyles.foldoutHeader);
            if (!_showAbilities) return;

            EditorGUI.indentLevel++;
            if (_target.Abilities.Count == 0) EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);

            foreach (var kv in _target.Abilities)
            {
                if (!IsMatchFilter(kv.Key)) continue;

                var ability = kv.Value;
                EditorGUILayout.BeginHorizontal("box");
                
                GUILayout.Label(ability.Name, EditorStyles.boldLabel, GUILayout.Width(150));
                GUILayout.FlexibleSpace();

                // 展示执行状态
                if (ability.IsExecuting)
                {
                    GUI.contentColor = new Color(1f, 0.6f, 0.2f); // 橘色代表正在释放中
                    GUILayout.Label("● Executing", EditorStyles.boldLabel);
                }
                else
                {
                    GUI.contentColor = Color.gray;
                    GUILayout.Label("○ Idle", EditorStyles.miniLabel);
                }
                GUI.contentColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        // --- 4. 反应 (Reactions) ---
        private void DrawReactions()
        {
            _showReactions = EditorGUILayout.Foldout(_showReactions, $"⚡ 被动反应 - Reactions ({_target.Reactions.Count})", true, EditorStyles.foldoutHeader);
            if (!_showReactions) return;

            EditorGUI.indentLevel++;
            if (_target.Reactions.Count == 0) EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);

            foreach (var kv in _target.Reactions)
            {
                if (!IsMatchFilter(kv.Key)) continue;
                
                var reaction = kv.Value;
                EditorGUILayout.BeginHorizontal("box");
                
                // 左侧显示名字和类名
                EditorGUILayout.BeginVertical();
                GUILayout.Label(reaction.Name, EditorStyles.boldLabel);
                GUILayout.Label(reaction.GetType().Name, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 右侧显示优先级和激活状态
                EditorGUILayout.BeginVertical();
                GUILayout.Label($"Priority: {reaction.Priority}", EditorStyles.miniLabel);
                
                // 绘制一个复选框展示状态
                GUI.enabled = false; // 仅供查看，暂不允许直接在面板修改
                EditorGUILayout.ToggleLeft("Active", reaction.IsActive, GUILayout.Width(60));
                GUI.enabled = true;
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        #endregion

        private void DrawLine(Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color);
        }
    }
}
#endif