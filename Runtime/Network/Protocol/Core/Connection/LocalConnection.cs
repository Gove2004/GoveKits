// === LocalConnection.cs ===
using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Network.Core
{
    public class LocalConnection : IConnection
    {
        public int ConnectionId { get; set; } = 0; // 本地主机玩家固定为 0
        public bool IsConnected { get; private set; } = true;

        // 客户端发给服务端的队列
        private readonly Queue<(int msgId, byte[] data)> _clientToServerQueue = new();
        // 服务端发给客户端的队列
        private readonly Queue<(int msgId, byte[] data)> _serverToClientQueue = new();

        // [IConnection 接口] 这是服务端调用，发给本地客户端的
        public void Send(int msgId, byte[] payload)
        {
            if (IsConnected) _serverToClientQueue.Enqueue((msgId, payload));
        }

        // 这是本地客户端调用，发给服务端的
        public void ClientSendToServer(int msgId, byte[] payload)
        {
            if (IsConnected) _clientToServerQueue.Enqueue((msgId, payload));
        }

        public void Disconnect() => IsConnected = false;

        // 在主线程 Update 里把队列里的消息吐出来
        public void PumpClient(Action<int, byte[]> onClientReceive)
        {
            while (_serverToClientQueue.Count > 0)
            {
                var msg = _serverToClientQueue.Dequeue();
                onClientReceive?.Invoke(msg.msgId, msg.data);
            }
        }

        public void PumpServer(Action<int, int, byte[]> onServerReceive)
        {
            while (_clientToServerQueue.Count > 0)
            {
                var msg = _clientToServerQueue.Dequeue();
                onServerReceive?.Invoke(ConnectionId, msg.msgId, msg.data);
            }
        }
    }
}