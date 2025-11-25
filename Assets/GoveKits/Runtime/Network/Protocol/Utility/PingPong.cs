using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Network
{

    public class PingPong : NetworkBehaviour
    {
        [Header("Config")]
        public float Interval = 2f;    // 发送频率
        public float Timeout = 10f;    // 超时阈值

        [Header("Client Stats")]
        public float LastRTT = -1f;    // 往返时延 (毫秒)
        
        // --- 客户端状态 ---
        private float _lastSendTime;
        private float _lastRecvTime;
        
        // --- 服务器状态 (记录所有客户端的最后活跃时间) ---
        private Dictionary<int, float> _clientKeepAlive = new Dictionary<int, float>();

        private void Start()
        {
            // 1. 订阅连接事件
            NetworkManager.Instance.OnClientConnected += OnClientConnected;
            NetworkManager.Instance.OnClientDisconnected += OnClientDisconnected;

            // 初始化计时器
            ResetTimers();
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance)
            {
                NetworkManager.Instance.Unbind(this);
                NetworkManager.Instance.OnClientConnected -= OnClientConnected;
                NetworkManager.Instance.OnClientDisconnected -= OnClientDisconnected;
            }
        }

        private void ResetTimers()
        {
            _lastSendTime = Time.time;
            _lastRecvTime = Time.time;
            _clientKeepAlive.Clear();
        }

        // --- 事件处理 ---

        private void OnClientConnected(int id)
        {
            // 如果我是客户端，且连接的是我自己，重置本地计时
            if (NetworkManager.Instance.IsClient && id == NetworkManager.Instance.MyPlayerID)
            {
                _lastSendTime = Time.time;
                _lastRecvTime = Time.time;
            }

            // 如果我是服务器，记录新客户端的时间
            if (NetworkManager.Instance.IsServer)
            {
                _clientKeepAlive[id] = Time.time;
            }
        }

        private void OnClientDisconnected(int id)
        {
            if (NetworkManager.Instance.IsServer)
            {
                _clientKeepAlive.Remove(id);
            }
        }

        // --- 主循环 ---

        private void Update()
        {
            if (!NetworkManager.Instance.IsConnected) return;

            float now = Time.time;

            // === 客户端逻辑：发包 + 检测服务器超时 ===
            if (NetworkManager.Instance.IsClient)
            {
                // 1. 定时发送 Ping
                if (now - _lastSendTime >= Interval)
                {
                    SendPing();
                    _lastSendTime = now;
                }

                // 2. 检测服务器是否挂了
                if (now - _lastRecvTime > Timeout)
                {
                    Debug.LogError($"[Heartbeat] Server Timeout! ({now - _lastRecvTime:F1}s > {Timeout}s)");
                    NetworkManager.Instance.Close();
                }
            }

            // === 服务器逻辑：检测客户端超时 ===
            if (NetworkManager.Instance.IsServer)
            {
                // 遍历检查所有客户端
                // 注意：不能在 foreach 中直接 Remove，收集需要断开的 ID
                List<int> timeoutClients = null;

                foreach (var kvp in _clientKeepAlive)
                {
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
                        // 这里调用 Manager 的底层方法断开连接
                        // 注意：你需要确保 Peer 层有公开的方法断开指定 ID，或者直接 Close
                        // 在之前的 ServerPeer 代码中，你可以调用 RemoveConnection(id)
                        // 但为了架构统一，通常 ServerPeer 检测到底层断开会自动处理。
                        // 这里我们只能做到逻辑上的剔除，或者扩展 NetworkManager 增加 Kick(id) 方法
                        
                        // 暂时从列表移除，实际项目建议在 NetworkManager 增加 KickPlayer(id)
                        _clientKeepAlive.Remove(id); 
                    }
                }
            }
        }

        // --- 消息处理 ---

        private void SendPing()
        {
            // 发送带有当前时间的包
            NetworkManager.Instance.SendToServer(new PingPongMessage(Time.realtimeSinceStartup));
        }


        [MessageHandler(Protocol.PingPongMsgID)]
        private void OnPingPong(PingPongMessage msg)
        {
            // Server: 原样弹回
            if (NetworkManager.Instance.IsServer)
            {
                _clientKeepAlive[msg.Header.SenderID] = Time.time;
                NetworkManager.Instance.SendToPlayer(msg.Header.SenderID, msg);
            }

            // Client: 计算 RTT
            if (NetworkManager.Instance.IsClient)
            {
                // 只有当这是服务器回给我的包时 (Host模式下要注意区分)
                // 简单处理：收到就更新
                _lastRecvTime = Time.time;
                
                // RTT = 当前时间 - 包里带的发送时间
                float rtt = (Time.realtimeSinceStartup - msg.Timestamp) * 1000f;
                
                // 平滑处理
                if (LastRTT < 0) LastRTT = rtt;
                else LastRTT = Mathf.Lerp(LastRTT, rtt, 0.2f);
            }
        }
    }



    // 消息定义保持不变
    [Message(Protocol.PingPongMsgID)]
    public class PingPongMessage : Message
    {
        public float Timestamp; // 发送时间戳

        public PingPongMessage() { }

        public PingPongMessage(float timestamp)
        {
            Timestamp = timestamp;
        }
        protected override int BodyLength() => 4;
        protected override void BodyWriting(byte[] buffer, ref int index) => WriteFloat(buffer, Timestamp, ref index);
        protected override void BodyReading(byte[] buffer, ref int index) => Timestamp = ReadFloat(buffer, ref index);
    }
}