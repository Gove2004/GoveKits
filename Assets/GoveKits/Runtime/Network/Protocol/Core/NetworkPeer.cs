using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public interface IPeer
    {
        bool IsAlive { get; }
        void Start(string address, int port);
        void Send(Message msg);
        void Stop();
        void Update();
    }

    // =========================================================
    // Client Peer
    // =========================================================
    public class ClientPeer : IPeer
    {
        public bool IsAlive => _serverConnection != null && _serverConnection.IsAlive;
        private NetworkConnection _serverConnection;
        private readonly MessageDispatcher _dispatcher;

        public ClientPeer(MessageDispatcher dispatcher) => _dispatcher = dispatcher;

        public async void Start(string ip, int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(ip, port);
                var transport = new TcpSocketTransport(socket);
                
                // Client 建立连接，此时不知道ID，但知道对面是 Server(0)
                _serverConnection = new NetworkConnection(NetworkManager.ServerID, transport, _dispatcher, false);
                // _serverConnection.OnDisconnected += NetworkManager.Instance.OnServerDisconnected;
                
                Debug.Log("[Client] Connected to Server.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Client] Connect Failed: {ex.Message}");
                NetworkManager.Instance.Close();
            }
        }

        public void Send(Message msg) => _serverConnection?.Send(msg);

        public void Stop() { _serverConnection?.Close(); _serverConnection = null; }
        public void Update() { }
    }

    // =========================================================
    // Host Peer (Refactored)
    // =========================================================
    public class HostPeer : IPeer
    {
        public bool IsAlive { get; protected set; }
        
        // Server端：所有连入的玩家连接 (PlayerID -> Connection)
        private readonly Dictionary<int, NetworkConnection> _playerConnections = new Dictionary<int, NetworkConnection>();
        
        // Client端：Host玩家自己通往Server逻辑的连接
        private NetworkConnection _localClientConnection; 

        private Socket _listener;
        private readonly MessageDispatcher _dispatcher;

        public HostPeer(MessageDispatcher dispatcher) => _dispatcher = dispatcher;

        public void Start(string ip, int port)
        {
            IsAlive = true;

            // 1. 启动 TCP 监听
            try
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Bind(new IPEndPoint(IPAddress.Any, port));
                _listener.Listen(NetworkManager.MaxConnections);
                AcceptLoop().Forget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Host] Start Failed: {ex.Message}");
                Stop();
                return;
            }

            // 建立回环
            var serverSideTransport = new LocalTransport();
            var clientSideTransport = new LocalTransport();
            serverSideTransport.ConnectTo(clientSideTransport);

            // A. Server 侧记录 (对面是 HostPlayer): isServerSide = true
            var connToHostPlayer = new NetworkConnection(NetworkManager.HostPlayerID, serverSideTransport, _dispatcher, true);
            AddPlayerConnection(NetworkManager.HostPlayerID, connToHostPlayer);

            // B. Client 侧记录 (对面是 Server): isServerSide = false
            _localClientConnection = new NetworkConnection(NetworkManager.ServerID, clientSideTransport, _dispatcher, false);
            
            Debug.Log("[Host] Loopback Established.");
        }

        private async UniTaskVoid AcceptLoop()
        {
            while (_listener != null)
            {
                try {
                    var clientSocket = await _listener.AcceptAsync();
                    int newId = NetworkManager.NextPlayerID++;
                    
                    var transport = new TcpSocketTransport(clientSocket);
                    
                    // Server 接收的外部连接: isServerSide = true
                    var conn = new NetworkConnection(newId, transport, _dispatcher, true);
                    
                    AddPlayerConnection(newId, conn);
                } catch { break; }
            }
        }

        private void AddPlayerConnection(int id, NetworkConnection conn)
        {
            lock (_playerConnections) _playerConnections[id] = conn;
            conn.OnDisconnected += () => RemovePlayerConnection(id);
            NetworkManager.Instance.NotifyClientConnected(id);
        }

        private void RemovePlayerConnection(int id)
        {
            lock (_playerConnections)
            {
                if (_playerConnections.Remove(id))
                {
                    NetworkManager.Instance.NotifyClientDisconnected(id);
                }
            }
        }

        // === 路由发送逻辑 ===
        
        // 发送单条消息
        public void Send(Message msg)
        {
            int target = msg.Header.TargetID;

            // 1. 如果 HostPlayer 发给 Server (Target=0)
            if (target == NetworkManager.ServerID)
            {
                _localClientConnection?.Send(msg);
                return;
            }

            // 2. 如果 Server 发给 任意玩家 (Target > 0)
            // (包括 Server 发给 HostPlayer 自己，也是走这里)
            lock (_playerConnections)
            {
                if (_playerConnections.TryGetValue(target, out var conn))
                {
                    conn.Send(msg);
                }
            }
        }

        // 广播 (带有排除列表)
        public void SendToAll(Message msg, int excludeId) 
            => SendToAll(msg, new HashSet<int> { excludeId });

        public void SendToAll(Message msg, HashSet<int> excludeIds)
        {
            lock (_playerConnections)
            {
                foreach (var kv in _playerConnections)
                {
                    int id = kv.Key;
                    if (excludeIds != null && excludeIds.Contains(id)) continue;
                    
                    kv.Value.Send(msg);
                }
            }
        }

        public void Stop()
        {
            IsAlive = false;
            try { _listener?.Close(); } catch { }
            _listener = null;

            lock (_playerConnections)
            {
                foreach (var c in _playerConnections.Values) c.Close();
                _playerConnections.Clear();
            }
            _localClientConnection?.Close();
        }

        public void Update() { }
    }

    // =========================================================
    // Offline Peer
    // =========================================================
    public class OfflinePeer : IPeer
    {
        public bool IsAlive => true;
        private MessageDispatcher _dispatcher;
        public OfflinePeer(MessageDispatcher dispatcher) => _dispatcher = dispatcher;
        public void Start(string address, int port) { }
        public void Send(Message msg)
        {
            // 离线模式：直接派发给自己
            UniTask.Void(async () => {
                await UniTask.Yield();
                await _dispatcher.DispatchAsync(msg);
            });
        }
        public void Stop() { }
        public void Update() { }
    }
}