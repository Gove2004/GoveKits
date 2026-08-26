using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    /// <summary>
    /// Base class for all GoveKits EditorWindow tools.
    /// Provides common UI helpers: DrawLine, header drawing, search toolbar, and optional auto-refresh.
    /// </summary>
    public abstract class GoveKitsEditorWindow : EditorWindow
    {
        /// <summary>Scroll position for the main content area.</summary>
        protected Vector2 _scrollPos;

        /// <summary>Search filter text.</summary>
        protected string _searchQuery = string.Empty;

        /// <summary>Whether auto-refresh is enabled.</summary>
        protected bool _autoRefresh = true;

        /// <summary>Next repaint timestamp.</summary>
        private double _nextRefreshTime;

        /// <summary>Auto-refresh interval in seconds.</summary>
        protected const double RefreshInterval = 0.5;

        /// <summary>Whether the default search toolbar (with auto-refresh toggle) should be drawn.
        /// Override to false if your subclass draws these controls elsewhere (e.g. in DrawHeader).</summary>
        protected virtual bool HasAutoRefreshToggle => true;

        /// <summary>Whether content should be wrapped in a ScrollView. Override to false
        /// if your subclass manages its own scroll layout (e.g. split-pane panels).</summary>
        protected virtual bool UseScrollView => true;

        /// <summary>Called from OnEnable. Override to initialize reflection caches or subscriptions.</summary>
        protected virtual void OnGoveWindowEnable() { }

        /// <summary>Called from OnDisable. Override to cleanup reflection subscriptions.</summary>
        protected virtual void OnGoveWindowDisable() { }

        /// <summary>Called from OnGUI after header and toolbar. Override to draw main content.</summary>
        protected abstract void OnGoveDrawContent();

        private void OnEnable()
        {
            OnGoveWindowEnable();
        }

        private void OnDisable()
        {
            OnGoveWindowDisable();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup > _nextRefreshTime)
            {
                _nextRefreshTime = EditorApplication.timeSinceStartup + RefreshInterval;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSearchToolbar();

            if (UseScrollView)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                OnGoveDrawContent();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                OnGoveDrawContent();
            }
        }

        /// <summary>
        /// Draws the window header with title and optional right-aligned action buttons.
        /// Subclasses can override to customize the header content.
        /// </summary>
        protected virtual void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(GetHeaderTitle(), EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            DrawHeaderButtons();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        /// <summary>Return the title shown in the window header.</summary>
        protected virtual string GetHeaderTitle()
        {
            return GetType().Name.Replace("Window", "");
        }

        /// <summary>Draw additional buttons on the right side of the header. Override to add custom buttons.</summary>
        protected virtual void DrawHeaderButtons() { }

        /// <summary>
        /// Draws the search toolbar with auto-refresh toggle and refresh button.
        /// Subclasses can override to add extra controls.
        /// </summary>
        protected virtual void DrawSearchToolbar()
        {
            EditorGUILayout.BeginHorizontal("box");

            EditorGUIUtility.labelWidth = 50;
            _searchQuery = EditorGUILayout.TextField("搜索:", _searchQuery, GUILayout.Height(20));
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(10);

            if (HasAutoRefreshToggle)
            {
                _autoRefresh = EditorGUILayout.ToggleLeft("自动刷新", _autoRefresh, GUILayout.Width(80));
                if (GUILayout.Button("刷新", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        /// <summary>
        /// Draws a horizontal separator line.
        /// Shared by all windows — replaces the duplicated method in each class.
        /// </summary>
        protected static void DrawLine(Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color ?? new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        /// <summary>
        /// Helper to enable auto-refresh mode. Call this from OnGoveWindowEnable
        /// if the window needs real-time updates during Play mode.
        /// </summary>
        protected void EnableAutoRefresh()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Shows the "not in play mode" info box and returns true to indicate
        /// the caller should skip drawing live data.
        /// </summary>
        protected bool DrawNotPlayingHint()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要在 Play 模式下才能查看实时数据。", MessageType.Info);
                return true;
            }
            return false;
        }
    }
}
