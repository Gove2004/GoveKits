using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Network;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    /// <summary>
    /// Network 网络监控窗口，同时展示 Client 和 Server 的连接状态。
    /// 通过反射读取 ClientCore 和 ServerCore 的内部字段，实时显示 RTT、连接数和会话详情。
    /// </summary>
    public class NetworkWindow : GoveKitsEditorWindow
    {
        /// <summary>网络监控的 Tab 枚举。</summary>
        private enum NetTab { Client, Server }

        /// <summary>当前激活的 Tab（客户端/服务端）。</summary>
        private NetTab _activeTab = NetTab.Client;

        /// <summary>通过反射缓存 ClientCore 内部 _session 字段的引用。</summary>
        private FieldInfo _clientSessionField;

        /// <summary>通过反射缓存 ClientCore 内部 _playerId 字段的引用。</summary>
        private FieldInfo _clientPlayerIdField;

        /// <summary>通过反射缓存 ClientCore 内部 _rtt 字段的引用。</summary>
        private FieldInfo _clientRttField;

        /// <summary>通过反射缓存 ServerCore 内部 _sessions 字段的引用。</summary>
        private FieldInfo _serverSessionsField;

        /// <summary>通过反射缓存 ServerCore 内部 _isListening 字段的引用。</summary>
        private FieldInfo _serverListeningField;

        /// <summary>
        /// 显示 Network 监控窗口。菜单路径: GoveKits/Network。
        /// </summary>
        [MenuItem("GoveKits/Network", false, 104)]
        public static void ShowWindow()
        {
            var window = GetWindow<NetworkWindow>("Network 监控");
            window.minSize = new Vector2(550, 450);
            window.Show();
        }

        protected override void OnGoveWindowEnable()
        {
            _clientSessionField = typeof(ClientCore).GetField("_session", BindingFlags.NonPublic | BindingFlags.Static);
            _clientPlayerIdField = typeof(ClientCore).GetField("_playerId", BindingFlags.NonPublic | BindingFlags.Static);
            _clientRttField = typeof(ClientCore).GetField("_rtt", BindingFlags.NonPublic | BindingFlags.Static);
            _serverSessionsField = typeof(ServerCore).GetField("_sessions", BindingFlags.NonPublic | BindingFlags.Static);
            _serverListeningField = typeof(ServerCore).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Static);
            EnableAutoRefresh();
        }

        protected override void OnGoveDrawContent()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要在 Play 模式下才能查看实时数据。", MessageType.Info);
                return;
            }

            if (_activeTab == NetTab.Client) DrawClientInfo();
            else DrawServerInfo();
        }

        protected override void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Network 网络监控", EditorStyles.largeLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            DrawLine();
        }

        protected override void DrawSearchToolbar()
        {
            _activeTab = (NetTab)GUILayout.Toolbar((int)_activeTab, new[] { "客户端", "服务端" }, GUILayout.Height(22));
            GUILayout.Space(5);
        }

        private void DrawClientInfo()
        {
            if (_clientSessionField == null)
            {
                EditorGUILayout.HelpBox("无法反射获取 ClientCore 字段。", MessageType.Error);
                return;
            }

            var session = _clientSessionField.GetValue(null) as Session;
            int playerId = (int)(_clientPlayerIdField?.GetValue(null) ?? 0);
            float rtt = (float)(_clientRttField?.GetValue(null) ?? 0f);

            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Client 状态", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("PlayerId:", GUILayout.Width(80));
            EditorGUILayout.LabelField(playerId.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("RTT:", GUILayout.Width(80));
            string rttStr = $"{rtt:F0} ms";
            var color = rtt < 50 ? Color.green : (rtt < 150 ? Color.yellow : Color.red);
            var defaultColor = GUI.contentColor;
            GUI.contentColor = color;
            EditorGUILayout.LabelField(rttStr);
            GUI.contentColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("连接状态:", GUILayout.Width(80));
            bool connected = session?.IsConnected == true;
            var connColor = connected ? Color.green : Color.red;
            GUI.contentColor = connColor;
            EditorGUILayout.LabelField(connected ? "已连接" : "未连接");
            GUI.contentColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (connected)
            {
                EditorGUILayout.HelpBox("连接正常", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("未连接到服务器", MessageType.Warning);
            }
        }

        private void DrawServerInfo()
        {
            if (_serverSessionsField == null)
            {
                EditorGUILayout.HelpBox("无法反射获取 ServerCore 字段。", MessageType.Error);
                return;
            }

            var sessions = _serverSessionsField.GetValue(null) as IDictionary;
            bool isListening = (bool)(_serverListeningField?.GetValue(null) ?? false);

            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Server 状态", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("监听状态:", GUILayout.Width(80));
            var listenColor = isListening ? Color.green : Color.red;
            var defaultColor = GUI.contentColor;
            GUI.contentColor = listenColor;
            EditorGUILayout.LabelField(isListening ? "已监听" : "未监听");
            GUI.contentColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (sessions != null)
            {
                EditorGUILayout.LabelField($"在线连接数: {sessions.Count}", EditorStyles.boldLabel);
                GUILayout.Space(5);

                foreach (DictionaryEntry kvp in sessions)
                {
                    int sessionId = (int)kvp.Key;
                    var session = kvp.Value as Session;
                    if (session == null) continue;

                    EditorGUILayout.BeginHorizontal("helpbox");
                    EditorGUILayout.LabelField($"连接 #{sessionId}", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"RTT: {session.RTT:F0} ms");
                    EditorGUILayout.LabelField(session.IsConnected ? "在线" : "掉线");

                    if (GUILayout.Button("踢出", GUILayout.Width(40)))
                    {
                        session.Kick("已从编辑器踢出");
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("当前没有客户端连接。", MessageType.Info);
            }
        }
    }
}
