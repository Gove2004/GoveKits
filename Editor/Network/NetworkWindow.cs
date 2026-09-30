using GoveKits.Runtime.Network;
using GoveKits.Runtime.Util;
using Mirror;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor
{
    /// <summary>
    /// Network 网络监控窗口（v3.0.0 起基于内置 Mirror）。
    /// 实时显示 Mirror 客户端/服务端连接状态、RTT 与连接列表。
    /// </summary>
    public class NetworkWindow : GoveKitsEditorWindow
    {
        /// <summary>网络监控的 Tab 枚举。</summary>
        private enum NetTab { Client, Server }

        /// <summary>当前激活的 Tab（客户端/服务端）。</summary>
        private NetTab _activeTab = NetTab.Client;

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
            EnableAutoRefresh();
        }

        protected override void OnGoveDrawContent()
        {
            if (DrawNotPlayingHint()) return;

            if (_activeTab == NetTab.Client) DrawClientInfo();
            else DrawServerInfo();
        }

        protected override void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Network 网络监控（Mirror）", EditorStyles.largeLabel);
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
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Client 状态", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            var defaultColor = GUI.contentColor;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("客户端激活:", GUILayout.Width(80));
            bool active = NetworkClient.active;
            GUI.contentColor = active ? Color.green : Color.red;
            EditorGUILayout.LabelField(active ? "是" : "否");
            GUI.contentColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("连接状态:", GUILayout.Width(80));
            bool connected = NetworkClient.active && NetworkClient.connection != null;
            GUI.contentColor = connected ? Color.green : Color.red;
            EditorGUILayout.LabelField(connected ? "已连接" : "未连接");
            GUI.contentColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("RTT:", GUILayout.Width(80));
            if (NetworkCore.IsClientConnected)
            {
                double rttMs = NetworkCore.Rtt * 1000.0;
                string rttStr = $"{rttMs:F0} ms";
                var color = rttMs < 50 ? Color.green : (rttMs < 150 ? Color.yellow : Color.red);
                GUI.contentColor = color;
                EditorGUILayout.LabelField(rttStr);
                GUI.contentColor = defaultColor;
            }
            else
            {
                // 未连接时 NetworkTime.rtt 恒为 0，直接显示会以"0 ms 绿色"伪装出网络良好的假象
                GUI.contentColor = Color.gray;
                EditorGUILayout.LabelField("—（未连接）");
                GUI.contentColor = defaultColor;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (connected)
                EditorGUILayout.HelpBox("连接正常", MessageType.Info);
            else
                EditorGUILayout.HelpBox("未连接到服务器", MessageType.Warning);
        }

        private void DrawServerInfo()
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField("Server 状态", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            var defaultColor = GUI.contentColor;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("服务端激活:", GUILayout.Width(80));
            bool active = NetworkServer.active;
            GUI.contentColor = active ? Color.green : Color.red;
            EditorGUILayout.LabelField(active ? "是" : "否");
            GUI.contentColor = defaultColor;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            int count = NetworkServer.connections?.Count ?? 0;
            EditorGUILayout.LabelField($"在线连接数: {count}", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (count > 0)
            {
                foreach (var kvp in NetworkServer.connections)
                {
                    var conn = kvp.Value;
                    if (conn == null) continue;

                    EditorGUILayout.BeginHorizontal("helpbox");
                    EditorGUILayout.LabelField($"连接 #{kvp.Key}", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField(conn.address, GUILayout.Width(140));
                    EditorGUILayout.LabelField(conn.isReady ? "就绪" : "未就绪");

                    if (GUILayout.Button("踢出", GUILayout.Width(40)))
                    {
                        conn.Disconnect();
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
