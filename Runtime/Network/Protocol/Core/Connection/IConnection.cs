// === IConnection.cs ===
using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Network
{
    public interface IConnection
    {
        int ConnectionId { get; set; }
        bool IsConnected { get; }
        
        // 发送消息：MsgId + 序列化后的 Protobuf byte[]
        void Send(int msgId, byte[] payload);
        
        void Disconnect();
    }
}