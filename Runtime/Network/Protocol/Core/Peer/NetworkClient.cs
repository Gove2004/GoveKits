// === NetworkClient.cs ===
using System;
using System.Net.Sockets;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network.Core
{
    public class NetworkClient
    {
        public IConnection Connection { get; private set; } // 可以是真TCP，也可以是Local
        private LocalConnection _localConn => Connection as LocalConnection;
        
        public event Action<int, byte[]> OnMessageReceived; // msgId, payload

        // ================== 启动方式 1：纯客户端连外部 ==================
        public void Connect(string ip, int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ip, port); // 阻塞连接，实际中可换异步
            
            var tcpConn = new TcpConnection(socket, -1);
            tcpConn.OnMessageReceived += (connId, msgId, payload) => OnMessageReceived?.Invoke(msgId, payload);
            tcpConn.OnDisconnected += (_) => Disconnect();
            
            Connection = tcpConn;
            
            LogCore.Success(nameof(NetworkClient), $"连接成功 {ip}:{port}");
        }

        // ================== 启动方式 2：作为 Host 的本地客户端 ==================
        public void ConnectLocal(LocalConnection localConn)
        {
            Connection = localConn;
            
            LogCore.Success(nameof(NetworkClient), $"连接成功 LocalHost");
        }

        public void Send(int msgId, byte[] payload)
        {
            if (Connection == null || !Connection.IsConnected) return;

            if (_localConn != null) 
                _localConn.ClientSendToServer(msgId, payload);
            else 
                Connection.Send(msgId, payload);
        }

        public void Disconnect() => Connection?.Disconnect();

        public void Update()
        {
            // 如果是 Host 模式，驱动本地队列
            _localConn?.PumpClient((msgId, payload) => OnMessageReceived?.Invoke(msgId, payload));
        }
    }
}