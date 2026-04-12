using System;
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
        private float _lastSendTime;
        private float _lastRecvTime;

        private void Start()
        {
            DispatcherCore.Bind(this);
            ClientCore.OnConnected += OnConnected;
            ClientCore.OnDisconnected += OnDisconnected;
        }

        private void OnDestroy()
        {
            DispatcherCore.Unbind(this);
            ClientCore.OnConnected -= OnConnected;
            ClientCore.OnDisconnected -= OnDisconnected;
        }

        private void OnConnected()
        {
            _lastRecvTime = Time.unscaledTime;
            LogCore.Success(nameof(HeartbeatComponent), "Connected. Starting Heartbeat.");
        }

        private void OnDisconnected(string reason)
        {
            RTT = -1;
        }

        private void Update()
        {
            if (!ClientCore.IsConnected) return;

            float now = Time.unscaledTime;

            // 超时检测
            if (now - _lastRecvTime > Timeout)
            {
                LogCore.Error(nameof(HeartbeatComponent), "Server Timeout!");
                ClientCore.Shutdown();
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
                ClientSendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ServerRecvTime = 0
            };
            ClientCore.Send(msg);
        }

        [MessageHandler]
        private void OnHeartbeatResponse(PingPongHeartbeatMsg msg)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastRecvTime = now;

            long rttMs = now - msg.ClientSendTime;

            if (RTT < 0) RTT = rttMs;
            else RTT = Mathf.Lerp(RTT, rttMs, 0.2f);

            LogCore.Debug("Heartbeat", $"Pong. RTT: {RTT:F1}ms");
        }
    }
}