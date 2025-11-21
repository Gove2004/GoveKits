

using UnityEngine;

namespace GoveKits.Network
{
    public class Heartbeat : MonoBehaviour
    {
        public float Interval = 5f;
        public float Timeout = 15f; // 新增：超时时间（超过多久没收到回复判定断开）
        
        private float lastSendTime = 0f;
        private float lastRecvTime = 0f; // 新增：最后一次收到心跳的时间
        
        // 保存 Handler 引用以便注销
        private IMessageHandler _handler; 

        public void Start()
        {
            lastSendTime = Time.time;
            lastRecvTime = Time.time;
            
            // 保存返回的 handler
            _handler = NetManager.Instance.Register(1, new MessageHandler<HeartbeatMessage>(OnReciveHeartbeat));
        }

        private void OnDestroy()
        {
            // 【关键修复】反注册，防止报错
            if (NetManager.Instance != null && _handler != null)
            {
                NetManager.Instance.Unregister(1, _handler);
            }
        }

        private void Update()
        {
            // 只有连接状态才发心跳
            if (!NetManager.Instance.IsConnected) return;

            // 1. 发送逻辑
            if (Time.time - lastSendTime >= Interval)
            {
                SendHeartbeat();
                lastSendTime = Time.time;
            }

            // 2. 【新增】超时检测逻辑
            // 如果 15秒 没收到回复，认为掉线，主动断开
            if (Time.time - lastRecvTime > Timeout)
            {
                Debug.LogWarning("[Heartbeat] Connection Timeout! Disconnecting...");
                NetManager.Instance.Close();
            }
        }

        private void SendHeartbeat()
        {
            var heartbeatMsg = MessageBuilder.Create(1);
            if (heartbeatMsg != null)
            {
                NetManager.Instance.Send(heartbeatMsg);
            }
        }

        private void OnReciveHeartbeat(Message msg)
        {
            lastRecvTime = Time.time; // 更新接收时间
            float rtt = Time.time - lastSendTime; // 简单计算 RTT
            Debug.Log($"[Heartbeat] RTT: {rtt * 1000f:F1} ms");
        }
    }


    // 心跳消息定义
    public class HeartbeatMessageData : BinaryData
    {
        // 心跳包可以为空，或者包含时间戳等信息
        public override int Length() => 0;

        public override void Reading(byte[] buffer, ref int index) { }
        public override void Writing(byte[] buffer, ref int index) { }
    }
    [NetMessage(1)]
    public class HeartbeatMessage : Message<HeartbeatMessageData>
    {
        public HeartbeatMessage() : base(new HeartbeatMessageData()) { }
        public HeartbeatMessage(HeartbeatMessageData data) : base(data) { }
    }
}
