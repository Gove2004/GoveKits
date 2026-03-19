using Generated;
using UnityEngine;


namespace GoveKits.Network
{
    public class HeartbeatComponent : MonoBehaviour
    {
        [Header("Config")]
        public float Interval = 3f;    // 发送间隔
        public float Timeout = 10f;    // 超时判定

        [Header("Stats")]
        public float RTT = 0f;         // 往返时延
        public bool IsConnected => NetworkManager.Instance.Client.IsConnected;

        private float _lastSendTime;
        private float _lastRecvTime;

        private void Start()
        {
            // 绑定消息回调
            NetworkManager.Instance.Dispatcher.Bind(this);
            
            // 监听连接事件
            NetworkManager.Instance.Client.OnConnected += OnConnected;
            NetworkManager.Instance.Client.OnDisconnected += OnDisconnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance == null) return;
            NetworkManager.Instance.Dispatcher.Unbind(this);
            NetworkManager.Instance.Client.OnConnected -= OnConnected;
            NetworkManager.Instance.Client.OnDisconnected -= OnDisconnected;
        }

        private void OnConnected()
        {
            _lastRecvTime = Time.unscaledTime;
            _lastSendTime = Time.unscaledTime;
            LogManager.Log("PingPong", "Connected. Starting Heartbeat.");
        }

        private void OnDisconnected()
        {
            RTT = -1;
        }

        private void Update()
        {
            if (!IsConnected) return;

            float now = Time.unscaledTime;

            // 1. 超时检测
            if (now - _lastRecvTime > Timeout)
            {
                LogManager.LogError("Heartbeat", $"Server Timeout! ({now - _lastRecvTime:F1}s > {Timeout}s)");
                NetworkManager.Instance.Disconnect();
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
            var msg = new PingPongHeartbeatMsg { Time = time };
            NetworkManager.Instance.Send(msg);
        }

        // 收到服务器回包 (Pong)
        [MessageHandler]
        private void OnHeartbeatResponse(PingPongHeartbeatMsg msg)
        {
            float now = Time.unscaledTime;
            _lastRecvTime = now;

            // 计算 RTT (当前时间 - 消息里的发送时间)
            float rtt = (now - msg.Time) * 1000f; // 毫秒
            
            // 平滑处理 RTT
            if (RTT < 0) RTT = rtt;
            else RTT = Mathf.Lerp(RTT, rtt, 0.2f); // 简单平滑
            
            LogManager.Log("Heartbeat", $"Pong. RTT: {RTT:F1}ms");
        }
    }
}