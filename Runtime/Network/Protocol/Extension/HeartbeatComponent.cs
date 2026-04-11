using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network
{
    public class HeartbeatComponent : MonoBehaviour
    {
        [Header("Config")]
        public float Interval = 5f;
        public float Timeout = 15f;

        [Header("Stats")]
        public float RTT = 0f;

        public ClientCore Client => CoreLocator.Client;

        private float _lastSendTime;
        private float _lastRecvTime;

        private void Start()
        {
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

            // 超时检测
            if (now - _lastRecvTime > Timeout)
            {
                CoreLocator.Log.Error(nameof(HeartbeatComponent), "Server Timeout!");
                Client.Shutdown();
                return;
            }

            // 定时发送
            if (now - _lastSendTime >= Interval)
            {
                SendHeartbeat(now);
                _lastSendTime = now;
            }
        }

        private void SendHeartbeat(float time)
        {
            var msg = new PingPongHeartbeatMsg
            {
                ClientSendTime = time,
                ServerRecvTime = 0
            };
            Client.Send(msg);
        }

        [MessageHandler]
        private void OnHeartbeatResponse(PingPongHeartbeatMsg msg)
        {
            float now = Time.unscaledTime;
            _lastRecvTime = now;

            // ✅ 纯客户端时间差
            float rttMs = (now - msg.ClientSendTime) * 1000f;

            if (RTT < 0) RTT = rttMs;
            else RTT = Mathf.Lerp(RTT, rttMs, 0.2f);

            CoreLocator.Log.Debug("Heartbeat", $"Pong. RTT: {RTT:F1}ms");
        }
    }
}