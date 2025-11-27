using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Network
{
    public class PingPong : NetworkBehaviour
    {
        [Header("Config")]
        public float Interval = 2f;    
        public float Timeout = 10f;    

        [Header("Client Stats")]
        public float LastRTT = -1f;    
        
        private float _lastSendTime;
        private float _lastRecvTime;
        
        private Dictionary<int, float> _clientKeepAlive = new Dictionary<int, float>();

        private void Start()
        {
            NetworkManager.Instance.OnServerConnectedEvent += OnServerConnected;
            NetworkManager.Instance.OnServerDisconnectedEvent += OnServerDisconnected;
            NetworkManager.Instance.OnClientConnectedEvent += OnClientConnected;
            NetworkManager.Instance.OnClientDisconnectedEvent += OnClientDisconnected;

            ResetTimers();
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Instance == null) return;
            NetworkManager.Instance.OnServerConnectedEvent -= OnServerConnected;
            NetworkManager.Instance.OnServerDisconnectedEvent -= OnServerDisconnected;
            NetworkManager.Instance.OnClientConnectedEvent -= OnClientConnected;
            NetworkManager.Instance.OnClientDisconnectedEvent -= OnClientDisconnected;
            base.OnDestroy();
        }

        private void ResetTimers()
        {
            // 使用 unscaledTime 防止游戏暂停导致心跳停止
            _lastSendTime = Time.unscaledTime;
            _lastRecvTime = Time.unscaledTime;
            _clientKeepAlive.Clear();
        }

        private void OnClientConnected(int id)
        {
            if (NetworkManager.Instance.IsHost)
            {
                _clientKeepAlive[id] = Time.unscaledTime;
            }
        }

        private void OnClientDisconnected(int id)
        {
            if (NetworkManager.Instance?.IsHost == true)
            {
                _clientKeepAlive.Remove(id);
            }
        }

        public void OnServerConnected()
        {
            Debug.Log("[Client]ResetTimersResetTimersResetTimersResetTimersResetTimersResetTimers");
            ResetTimers();
        }

        public void OnServerDisconnected()
        {
            // 清理状态
            ResetTimers();
        }

        private void Update()
        {
            if (!NetworkManager.Instance.IsConnected) return;
            float now = Time.unscaledTime; // 改用 unscaledTime

            // === 客户端逻辑 ===
            if (NetworkManager.Instance.IsClient)
            {
                // 检测超时
                if (now - _lastRecvTime > Timeout)
                {
                    Debug.LogError($"[Heartbeat] Server Timeout! ({now - _lastRecvTime:F1}s > {Timeout}s)");
                    NetworkManager.Instance.Close();
                    return;
                }
    
                if (now - _lastSendTime >= Interval)
                {
                    Ping();
                    _lastSendTime = now;
                }
            }

            // === 服务器逻辑 ===
            if (NetworkManager.Instance.IsHost)
            {
                List<int> timeoutClients = null;

                foreach (var kvp in _clientKeepAlive)
                {
                    // Host 不需要踢掉自己 (ID=1)
                    if (kvp.Key == NetworkManager.HostPlayerID) continue;

                    if (now - kvp.Value > Timeout)
                    {
                        if (timeoutClients == null) timeoutClients = new List<int>();
                        timeoutClients.Add(kvp.Key);
                    }
                }

                if (timeoutClients != null)
                {
                    foreach (int id in timeoutClients)
                    {
                        Debug.LogWarning($"[Heartbeat] Client {id} Timeout. Kicking...");
                        NetworkManager.Instance.KickPlayer(id); 
                        _clientKeepAlive.Remove(id); 
                    }
                }
            }
        }

        private void Ping()
        {
            Debug.Log("[Heartbeat] Ping");
            NetworkManager.Instance.SendToServer(new PingPongMessage(Time.unscaledTime));
        }

        [MessageHandler(Protocol.PingPongMsgID)]
        private void Pong(PingPongMessage msg)
        {
            Debug.Log("[Heartbeat] Pong");
            float now = Time.unscaledTime;

            // === Server 端处理 ===
            if (NetworkManager.Instance.IsHost)
            {
                int senderId = msg.Header.SenderID;
                
                // 刷新该 Client 的保活时间
                _clientKeepAlive[senderId] = now;

                // Host 收到自己发的包，不要回复，否则死循环
                if (senderId != NetworkManager.HostPlayerID)
                {
                    NetworkManager.Instance.SendToPlayer(senderId, msg);
                }
                else 
                {
                    // 如果是 Host 自己发的 Ping，直接在这里作为 Pong 接收处理 RTT 相当于本地回环立即完成
                }
            }

            // === Client 端处理 ===
            if (NetworkManager.Instance.IsClient)
            {
                // 收到服务器回包（或者是 Host 本地直接处理）
                _lastRecvTime = now;
                
                // 计算 RTT
                float rtt = (now - msg.Timestamp) * 1000f;
                if (LastRTT < 0) LastRTT = rtt;
                else LastRTT = Mathf.Lerp(LastRTT, rtt, 0.2f);
            }
        }
    }
}