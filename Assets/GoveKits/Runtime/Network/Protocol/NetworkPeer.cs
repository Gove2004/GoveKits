using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{

    // === Peer 策略接口 ===
    public interface IPeer
    {
        bool IsAlive { get; }
        // 启动
        void Start(string address, int port);
        // 发送 (路由逻辑由 Peer 内部根据 TargetID 决定)
        void Send(Message msg);
        // 停止
        void Stop();
        // 轮询
        void Update();
    }



    // === 1. Client Peer ===
    public class ClientPeer : IPeer
    {
        public bool IsAlive => _connection != null && _connection.IsAlive;
        private NetworkConnection _connection;
        private MessageDispatcher _dispatcher;

        public ClientPeer(MessageDispatcher dispatcher) => _dispatcher = dispatcher;

        public async void Start(string ip, int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(ip, port);
                var transport = new TcpSocketTransport(socket);
                // Client暂用临时ID，由Server分配正式ID
                _connection = new NetworkConnection(NetworkManager.ClientTempID, transport, _dispatcher);
                _connection.OnDisconnected += () => NetworkManager.Instance.OnConnectionClose(NetworkManager.ClientTempID);
                Debug.Log("[Client] Connected.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Client] Connect Failed: {ex.Message}");
                NetworkManager.Instance.Close();
            }
        }

        public void Send(Message msg) => _connection?.Send(msg); // 客户端只发给服务器

        public void Stop() { _connection?.Close(); _connection = null; }
        public void Update() { }
    }

    // === 2. Server Peer ===
    public class ServerPeer : IPeer
    {
        public bool IsAlive { get; protected set; }
        protected Dictionary<int, NetworkConnection> _connections = new Dictionary<int, NetworkConnection>();
        protected MessageDispatcher _dispatcher;
        private Socket _listener;

        public ServerPeer(MessageDispatcher dispatcher) => _dispatcher = dispatcher;

        public virtual void Start(string ip, int port)
        {
            IsAlive = true;
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Any, port));
            _listener.Listen(NetworkManager.MaxConnections);
            AcceptLoop().Forget();
            Debug.Log($"[Server] Listening on {port}...");
        }

        private async UniTaskVoid AcceptLoop()
        {
            while (IsAlive && _listener != null)
            {
                try
                {
                    var clientSocket = await _listener.AcceptAsync();
                    int newId = NetworkManager.NextPlayerID++;
                    
                    var transport = new TcpSocketTransport(clientSocket);
                    var conn = new NetworkConnection(newId, transport, _dispatcher);
                    
                    AddConnection(newId, conn);
                }
                catch { break; }
            }
        }

        protected void AddConnection(int id, NetworkConnection conn)
        {
            lock (_connections) _connections[id] = conn;
            conn.OnDisconnected += () => RemoveConnection(id);
            // 触发 Manager 事件
            NetworkManager.Instance.NotifyClientConnected(id);
        }

        public void RemoveConnection(int id)
        {
            lock (_connections)
            {
                if (_connections.Remove(id))
                {
                    NetworkManager.Instance.NotifyClientDisconnected(id);
                    Debug.Log($"[Server] Client {id} disconnected.");
                }
            }
        }

        public virtual void Send(Message msg)
        {
            int targetId = msg.Header.TargetID;

            // 广播
            if (targetId == -1)
            {
                lock (_connections)
                {
                    foreach (var conn in _connections.Values)
                        conn.Send(msg);
                }
            }
            // 单发
            else if (targetId > 0)
            {
                lock (_connections)
                {
                    if (_connections.TryGetValue(targetId, out var conn))
                        conn.Send(msg);
                }
            }
        }

        public virtual void Stop()
        {
            IsAlive = false;
            _listener?.Close();
            lock (_connections)
            {
                foreach (var c in _connections.Values) c.Close();
                _connections.Clear();
            }
        }
        public void Update() { }
    }

    // === 3. Host Peer (策略核心：混合模式) ===
    public class HostPeer : ServerPeer
    {
        private NetworkConnection _localClientConn; // Host 扮演客户端的连接

        public HostPeer(MessageDispatcher dispatcher) : base(dispatcher) { }

        public override void Start(string ip, int port)
        {
            // 1. 启动服务器监听
            base.Start(ip, port);

            // 2. 建立本地双向管道
            var serverTransport = new LocalTransport();
            var clientTransport = new LocalTransport();
            serverTransport.ConnectTo(clientTransport);

            // 3. 服务器端增加一个 Connection (代表 Host 玩家)
            var serverConn = new NetworkConnection(NetworkManager.HostPlayerID, serverTransport, _dispatcher);
            AddConnection(NetworkManager.HostPlayerID, serverConn);

            // 4. 客户端端持有一个 Connection (发给服务器)
            _localClientConn = new NetworkConnection(NetworkManager.HostPlayerID, clientTransport, _dispatcher);
            
            Debug.Log("[Host] Local Loopback Started.");
        }

        public override void Send(Message msg)
        {
            // === 智能路由逻辑 ===
            
            // 如果发送者是 Host 玩家自己 (SenderID == HostPlayerID)
            // 说明这是“客户端行为”，应该走 LocalClient 发给 Server
            if (msg.Header.SenderID == NetworkManager.HostPlayerID)
            {
                _localClientConn?.Send(msg);
            }
            // 否则，这是“服务器行为”，走基类的 Server 发送逻辑 (广播或发给特定 TCP 客户端)
            else
            {
                base.Send(msg);
            }
        }

        public override void Stop()
        {
            base.Stop();
            _localClientConn?.Close();
        }
    }

    // === 4. Offline Peer ===
    public class OfflinePeer : IPeer
    {
        public bool IsAlive => true;
        private MessageDispatcher _dispatcher;
        public OfflinePeer(MessageDispatcher d) => _dispatcher = d;
        public void Start(string a, int p) { }
        public void Send(Message msg)
        {
            // 直接回环
            UniTask.Void(async () => {
                await UniTask.Yield();
                await _dispatcher.DispatchAsync(msg);
            });
        }
        public void Stop() { }
        public void Update() { }
    }
}