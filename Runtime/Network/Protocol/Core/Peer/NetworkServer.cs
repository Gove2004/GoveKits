// === NetworkServer.cs ===
using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network.Core
{
    public class NetworkServer
    {
        public Dictionary<int, IConnection> Connections = new();
        private int _nextConnId = 1; // 真实玩家从 1 开始，0 留给主机自己

        public event Action<int, int, byte[]> OnMessageReceived; // connId, msgId, payload
        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;

        // 这里应有一个 TcpListener 负责接收真实玩家连入
        // 伪代码: _tcpServer.OnAccepted += AddRealConnection;

        public void Start(int port)
        {
            // _tcpServer.Start(port);
            LogCore.Success(nameof(NetworkClient), $"启动服务器 {port}");
        }

        public void AddLocalConnection(LocalConnection localConn)
        {
            localConn.ConnectionId = 0;
            Connections[0] = localConn;
            OnClientConnected?.Invoke(0);
        }

        public void AddRealConnection(TcpConnection realConn)
        {
            int id = _nextConnId++;
            realConn.ConnectionId = id;
            
            realConn.OnMessageReceived += (cId, mId, p) => OnMessageReceived?.Invoke(cId, mId, p);
            realConn.OnDisconnected += (cId) => RemoveConnection(cId);
            
            Connections[id] = realConn;
            OnClientConnected?.Invoke(id);
        }

        private void RemoveConnection(int connId)
        {
            if (Connections.Remove(connId))
                OnClientDisconnected?.Invoke(connId);
        }

        // 帧同步最常用的方法：广播
        public void Broadcast(int msgId, byte[] payload, int excludeId = -1)
        {
            foreach (var kvp in Connections)
            {
                if (kvp.Key == excludeId) continue;
                kvp.Value.Send(msgId, payload);
            }
        }

        public void Update()
        {
            // 驱动主机模式的服务端收取队列
            if (Connections.TryGetValue(0, out var conn) && conn is LocalConnection localConn)
            {
                localConn.PumpServer((cId, mId, p) => OnMessageReceived?.Invoke(cId, mId, p));
            }
        }
    }
}