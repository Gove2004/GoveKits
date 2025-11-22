

using UnityEngine;

namespace GoveKits.Network
{
    public class Heartbeat : NetworkBehaviour
    {
        public const int HeartbeatMsgID = 0;
        public float Interval = 5f;
        public float Timeout = 15f; // 新增：超时时间（超过多久没收到回复判定断开）
        
        private float lastSendTime = 0f;
        private float lastRecvTime = 0f; // 新增：最后一次收到心跳的时间

        public void Start()
        {
            lastSendTime = Time.time;
            lastRecvTime = Time.time;
        }


        private void Update()
        {
            // 只有连接状态才发心跳
            if (!NetManager.Instance.IsConnected) return;

            // 1. 发送逻辑
            if (Time.time - lastSendTime >= Interval)
            {
                Ping();
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


        private void Ping()
        {
            NetManager.Instance.Send(MessageBuilder.Create<HeartbeatMessage>(HeartbeatMsgID));
        }


        [MessageHandler(HeartbeatMsgID)]
        private void Pong(HeartbeatMessage msg)
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
    [Message(Heartbeat.HeartbeatMsgID)]
    public class HeartbeatMessage : Message<HeartbeatMessageData> {}
    
}
