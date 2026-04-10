using GoveKits.Runtime.Core;
using UnityEngine;


namespace GoveKits.Runtime.Network
{
    public class HeartbeatComponent : MonoBehaviour
    {
        [Header("Config")]
        public float Interval = 5f;    // 发送间隔
        public float Timeout = 15f;    // 超时判定

        [Header("Stats")]
        public float RTT = 0f;         // 往返时延
        public ClientCore Client => CoreLocator.Client;

        private float _lastSendTime;
        private float _lastRecvTime;

        private void Start()
        {
            // 监听连接事件
            Client.OnConnected += OnConnected;
            Client.OnDisconnected += OnDisconnected;
        }

        private void OnDestroy()
        {
            Client.OnConnected -= OnConnected;
            Client.OnDisconnected -= OnDisconnected;
        }

        private void OnConnected()
        {
            _lastRecvTime = Time.unscaledTime;
            _lastSendTime = Time.unscaledTime;
            CoreLocator.Log.Success(nameof(HeartbeatComponent), "Connected. Starting Heartbeat.");
        }

        private void OnDisconnected(string reason)
        {
            RTT = -1;
        }

        private void Update()
        {
            if (!Client.IsConnected) return;

            float now = Time.unscaledTime;

            // 1. 超时检测
            if (now - _lastRecvTime > Timeout)
            {
                CoreLocator.Log.Error(nameof(HeartbeatComponent), $"Server Timeout! ({now - _lastRecvTime:F1}s > {Timeout}s)");
                Client.Shutdown();
                return;
            }

            // 2. 定时发送 Ping
            if (now - _lastSendTime >= Interval)
            {
                SendHeartbeat(now);
                _lastSendTime = now;
            }
        }

        private void SendHeartbeat(float time)
        {
            var msg = new PingPongHeartbeatMsg { ClientSendTime = time };
            Client.Send(msg);
        }

        // 收到服务器回包 (Pong)
        [MessageHandler]
        private void OnHeartbeatResponse(PingPongHeartbeatMsg msg)
        {
            float now = Time.unscaledTime;
            _lastRecvTime = now;

            // 计算 RTT (当前时间 - 消息里的发送时间)
            float rtt = (now - msg.ClientSendTime) * 1000f; // 毫秒
            
            // 平滑处理 RTT
            if (RTT < 0) RTT = rtt;
            else RTT = Mathf.Lerp(RTT, rtt, 0.2f); // 简单平滑
            
            CoreLocator.Log.Debug("Heartbeat", $"Pong. RTT: {RTT:F1}ms");
        }
    }
}