

using GoveKits.Save;
using UnityEngine;

namespace GoveKits.Network
{
    public class PingPong : NetworkBehaviour
    {
        public float Interval = 5f;
        public float Timeout = 15f; // 新增：超时时间（超过多久没收到回复判定断开）
        
        private float lastSendTime = 0f;
        private float lastRecvTime = 0f; // 新增：最后一次收到心跳的时间

        public float LastRTT { get; private set; } = -1f;


        public void Start()
        {
            lastSendTime = Time.time;
            lastRecvTime = Time.time;
        }


        private void Update()
        {
            // 只有连接状态才发心跳
            if (!NetworkManager.Instance.IsConnected) return;

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
                NetworkManager.Instance.Close();
            }
        }


        private void Ping()
        {
            NetworkManager.Instance.Send(MessageBuilder.Create<PingPongMessage>(Protocol.PingPongMsgID));
        }


        [MessageHandler(Protocol.PingPongMsgID)]
        private void Pong(PingPongMessage msg)
        {
            lastRecvTime = Time.time; // 更新接收时间
            LastRTT = Time.time - lastSendTime; // 简单计算 RTT
            Debug.Log($"[Heartbeat] RTT: {LastRTT * 1000f:F1} ms");
        }
    }



    // 心跳消息定义
    [Message(Protocol.PingPongMsgID)]
    public class PingPongMessage : EmptyMessage {}
}
