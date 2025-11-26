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
        void Start(string address, int port);
        void Send(Message msg);
        void Stop();
        void Update();
    }

    // =========================================================
    // 1. Client Peer: 纯客户端
    // =========================================================
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
                
                // Client暂用临时ID，等待接收 HelloMessage 更新为正式ID
                _connection = new NetworkConnection(NetworkManager.ClientTempID, transport, _dispatcher);
                
                // 监听断开
                _connection.OnDisconnected += () => NetworkManager.Instance.OnConnectionClose(NetworkManager.ClientTempID);
                
                Debug.Log("[Client] Socket Connected. Waiting for ID...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Client] Connect Failed: {ex.Message}");
                NetworkManager.Instance.Close();
            }
        }

        public void Send(Message msg)
        {
            // 客户端只有一个去处：发给服务器
            _connection?.Send(msg);
        }

        public void Stop() { _connection?.Close(); _connection = null; }
        public void Update() { }
    }

    // =========================================================
    // 2. Host Peer: 既是服务器，也是本地客户端
    // =========================================================
    public class HostPeer : IPeer
    {
        public bool IsAlive { get; protected set; }
        
        // --- 服务端部分 ---
        // 所有连入的连接 (包括 Host 自己的本地连接 和 外部 TCP 连接)
        private Dictionary<int, NetworkConnection> _connections = new Dictionary<int, NetworkConnection>();
        private Socket _listener;
        
        // --- 客户端部分 ---
        // Host 玩家通往服务器的专用本地管道
        private NetworkConnection _localClientConn; 
        
        private MessageDispatcher _dispatcher;

        public HostPeer(MessageDispatcher dispatcher) => _dispatcher = dispatcher;

        public void Start(string ip, int port)
        {
            IsAlive = true;

            // 1. 启动 TCP 监听 (等待外部玩家)
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Bind(new IPEndPoint(IPAddress.Any, port));
                _listener.Listen(NetworkManager.MaxConnections);
                AcceptLoop().Forget();
                Debug.Log($"[Host] Listening on port {port}...");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Host] Start Failed: {ex.Message}");
                Stop();
                return;
            }

            // 2. 建立本地回环 (Host 自己连自己)
            var serverTransport = new LocalTransport();
            var clientTransport = new LocalTransport();
            serverTransport.ConnectTo(clientTransport);

            // Server侧持有的连接 (ID = 1)
            var serverConn = new NetworkConnection(NetworkManager.HostPlayerID, serverTransport, _dispatcher);
            AddConnection(NetworkManager.HostPlayerID, serverConn);

            // Client侧持有的连接
            _localClientConn = new NetworkConnection(NetworkManager.HostPlayerID, clientTransport, _dispatcher);
            
            Debug.Log("[Host] Local Loopback Started.");
        }

        private async UniTaskVoid AcceptLoop()
        {
            while (IsAlive && _listener != null)
            {
                try
                {
                    var clientSocket = await _listener.AcceptAsync();
                    
                    // 分配新 ID
                    int newId = NetworkManager.NextPlayerID++;
                    
                    var transport = new TcpSocketTransport(clientSocket);
                    var conn = new NetworkConnection(newId, transport, _dispatcher);
                    
                    AddConnection(newId, conn);
                }
                catch { break; }
            }
        }

        private void AddConnection(int id, NetworkConnection conn)
        {
            lock (_connections) _connections[id] = conn;
            conn.OnDisconnected += () => RemoveConnection(id);
            
            // 通知 Manager 有新连接 (Manager 会判断是否需要发送 Hello)
            NetworkManager.Instance.NotifyClientConnected(id);
        }

        private void RemoveConnection(int id)
        {
            lock (_connections)
            {
                if (_connections.Remove(id))
                {
                    NetworkManager.Instance.NotifyClientDisconnected(id);
                    Debug.Log($"[Host] Client {id} disconnected.");
                }
            }
        }
        
        // 供 Manager 广播 RPC 使用
        public Dictionary<int, NetworkConnection> GetConnectionsCopy()
        {
            lock (_connections) return new Dictionary<int, NetworkConnection>(_connections);
        }

        // === 核心发送逻辑 ===
        public void Send(Message msg)
        {
            int targetId = msg.Header.TargetID;

            // 场景 A: Host 玩家发给服务器 (Client -> Server)
            if (targetId == NetworkManager.ServerID) 
            {
                _localClientConn?.Send(msg);
                return;
            }

            // 场景 B: 服务器发给客户端 (Server -> Client)
            // 包括发给 Host 自己 (ID=1) 和 外部 TCP 客户端 (ID=100+)
            
            if (targetId == -1) // 广播
            {
                lock (_connections)
                {
                    foreach (var conn in _connections.Values) 
                        conn.Send(msg);
                }
            }
            else if (targetId > 0) // 单发
            {
                lock (_connections)
                {
                    if (_connections.TryGetValue(targetId, out var conn)) 
                        conn.Send(msg);
                    else
                        Debug.LogWarning($"[Host] Target connection {targetId} not found.");
                }
            }
        }

        public void Stop()
        {
            IsAlive = false;
            
            // 关闭监听
            try { _listener?.Close(); } catch { }
            _listener = null;

            // 关闭所有服务端持有的连接
            lock (_connections)
            {
                var list = new List<NetworkConnection>(_connections.Values);
                foreach (var c in list) c.Close();
                _connections.Clear();
            }

            // 关闭本地客户端连接
            _localClientConn?.Close();
            _localClientConn = null;
        }

        public void Update() { }
    }

    // =========================================================
    // 3. Offline Peer: 离线模式
    // =========================================================
    public class OfflinePeer : IPeer
    {
        public bool IsAlive => true;
        private MessageDispatcher _dispatcher;
        public OfflinePeer(MessageDispatcher dispatcher) => _dispatcher = dispatcher;
        public void Start(string address, int port) { }
        public void Send(Message msg)
        {
            // 立即回环给自己
            UniTask.Void(async () => {
                await UniTask.Yield();
                await _dispatcher.DispatchAsync(msg);
            });
        }
        public void Stop() { }
        public void Update() { }
    }
}