using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public enum NetworkMode { Offline, Client, Server, Host }

    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        public const int ServerPlayerID = 0;  // 服务器 ID 固定为 0
        public const int HostPlayerID = 1;  // Host 在本地运行的服务器和客户端，ID 固定为 1
        public const int ClientTempID = -1;  // 临时 ID，等待服务器分配
        public static int NextPlayerID = 100;
        public const int MaxConnections = 32;


        // --- 配置 ---
        public string RemoteIP = "127.0.0.1";
        public int Port = 12345;
        [Tooltip("启用后，启动时作为客户端自动连接到服务器")]
        public bool AutoConnect = false;

        // --- 状态 ---
        public NetworkMode Mode { get; private set; } = NetworkMode.Offline;
        public int MyPlayerID { get; private set; } = 0;
        public bool IsConnected => Mode != NetworkMode.Offline;
        public bool IsHost => Mode == NetworkMode.Host;
        public bool IsServer => Mode == NetworkMode.Server;

        // --- 组件 ---
        private ITransport _transport; // 核心：持有传输层接口
        private readonly MessageDispatcher _dispatcher = new MessageDispatcher();

        // --- 连接池 ---
        private readonly List<IConnection> _connections = new List<IConnection>();
        private readonly Dictionary<int, IConnection> _connMap = new Dictionary<int, IConnection>();
        private IConnection _myConnection; // 客户端或Host的本地连接

        // --- 事件 ---
        public event Action<int> OnPlayerJoined;
        public event Action<int> OnPlayerLeft;
        public event Action OnDisconnected;

        protected void Awake()
        {
            // 初始化传输层 (未来可在此切换 KcpTransport)
            _transport = new TcpTransport(); 
            
            _dispatcher.Bind(this); // 绑定内置消息(Init)
            MessageBuilder.AutoRegisterAll();

            // 开启客户端自动连接
            if (AutoConnect)
            {
                StartClient();
            }
        }

        // ================== 启动流程 ==================

        public void StartHost()
        {
            Close();
            Mode = NetworkMode.Host;
            MyPlayerID = HostPlayerID;

            // 1. 启动服务器监听 (对外)
            _transport.StartServer(Port, OnNewConnectionCreated);

            // 2. 建立本地连接 (对内)
            var localConn = new LocalConnection(MyPlayerID);
            OnNewConnectionCreated(localConn);

            Debug.Log("[NetManager] Host Started.");
            OnPlayerJoined?.Invoke(MyPlayerID);
        }

        public void StartServer()
        {
            Close();
            Mode = NetworkMode.Server;
            MyPlayerID = ServerPlayerID;
            _transport.StartServer(Port, OnNewConnectionCreated);
            Debug.Log("[NetManager] Dedicated Server Started.");
        }

        public void StartClient()
        {
            Close();
            Mode = NetworkMode.Client;
            _transport.ConnectClient(RemoteIP, Port,
                onConnected: (conn) => 
                {
                    OnNewConnectionCreated(conn);
                    Debug.Log("[NetManager] Client Connected, Waiting for ID...");
                },
                onFailure: () => OnDisconnected?.Invoke()
            );
        }

        // ================== 连接管理 ==================

        // 统一处理所有新连接 (无论是 TCP 进来的还是 Local 创建的)
        private void OnNewConnectionCreated(IConnection conn)
        {
            _connections.Add(conn);
            if (conn.Id > 0) _connMap[conn.Id] = conn;

            // 订阅事件
            conn.OnMessageReceived += HandleMessage;
            conn.OnDisconnected += HandleDisconnect;

            // 记录自己的连接
            if (Mode == NetworkMode.Client) _myConnection = conn;
            if (Mode == NetworkMode.Host && conn is LocalConnection) _myConnection = conn;

            // 如果是 Server/Host 收到新连接，发送 ID
            if ((IsServer || IsHost) && conn.Id > 1) // 排除Host自己
            {
                Debug.Log($"[Server] Client Joined: {conn.Id}");
                conn.Send(new PlayerInitMessage { PlayerID = conn.Id });
                OnPlayerJoined?.Invoke(conn.Id);
            }
        }

        private void HandleDisconnect(int id)
        {
            if (_connMap.TryGetValue(id, out var conn))
            {
                conn.OnMessageReceived -= HandleMessage;
                conn.OnDisconnected -= HandleDisconnect;
                _connections.Remove(conn);
                _connMap.Remove(id);
            }
            OnPlayerLeft?.Invoke(id);
        }

        // ================== 消息分发 ==================

        private void HandleMessage(Message msg, int senderId)
        {
            // 1. 修正 ID
            msg.Header.SenderID = senderId;

            // 2. 转发逻辑 (Server/Host)
            if (IsServer || IsHost)
            {
                Broadcast(msg, senderId); // 排除发送者
            }

            // 3. 业务逻辑 (Dispatch)
            // Client: 处理来自 Server 的包
            // Host: 处理来自 Local 和 TCP Client 的包
            _dispatcher.DispatchAsync(msg).Forget();
        }

        // ================== 发送接口 ==================

        public void Send(Message msg)
        {
            if (!IsConnected) return;
            msg.Header.SenderID = MyPlayerID;

            if (Mode == NetworkMode.Client)
            {
                _myConnection?.Send(msg);
            }
            else
            {
                // Host/Server 发送 = 广播
                Broadcast(msg, MyPlayerID); 

                // Host 特殊处理：如果是 Host 发的，且是 Host 模式，
                // 上面的 Broadcast 排除了自己，所以这里要手动推给自己 (Loopback)
                if (IsHost)
                {
                    _myConnection?.Send(msg);
                }
            }
        }

        /// <summary>
        /// 发送消息给指定玩家
        /// </summary>
        /// <param name="playerId">目标玩家ID</param>
        /// <param name="msg">消息内容</param>
        public void SendTo(int playerId, Message msg)
        {
            if (!IsConnected) return;
            msg.Header.SenderID = MyPlayerID;

            if (_connMap.TryGetValue(playerId, out var conn))
            {
                conn.Send(msg);
            }
            else
            {
                Debug.LogWarning($"[NetManager] SendTo: Player {playerId} not found.");
            }
        }

        /// <summary>
        /// 广播消息给所有连接，排除指定玩家
        /// </summary>
        /// <param name="msg">消息内容</param>
        /// <param name="excludeId">排除的玩家ID</param>
        public void Broadcast(Message msg, int excludeId = -1)
        {
            for (int i = _connections.Count - 1; i >= 0; i--)
            {
                var conn = _connections[i];
                if (conn.IsConnected && conn.Id != excludeId)
                {
                    conn.Send(msg);
                }
            }
        }

        // ================== 系统消息 ==================

        [MessageHandler(Protocol.PlayerInitID)]
        private void OnReceiveInit(PlayerInitMessage msg)
        {
            if (Mode == NetworkMode.Client)
            {
                // 更新 ID
                int oldId = 0;
                MyPlayerID = msg.PlayerID;
                
                // 修正 Connection 映射
                if (_connMap.ContainsKey(oldId)) _connMap.Remove(oldId);
                _connMap[MyPlayerID] = _myConnection;

                Debug.Log($"[Client] ID Assigned: {MyPlayerID}");
                OnPlayerJoined?.Invoke(MyPlayerID);
            }
        }

        public void Close()
        {
            _transport?.Shutdown();
            foreach (var c in _connections) c.Close();
            _connections.Clear();
            _connMap.Clear();
            Mode = NetworkMode.Offline;
        }

        public void Bind(object t) => _dispatcher.Bind(t);
        public void Unbind(object t) => _dispatcher.Unbind(t);

        protected override void OnDestroy()
        {
            Close();
            base.OnDestroy();
        }
    }




    [Message(Protocol.PlayerInitID)]
    public class PlayerInitMessage : Message 
    {
        public int PlayerID;
        protected override int BodyLength() => 4;
        protected override void BodyWriting(byte[] b, ref int i) => WriteInt(b, PlayerID, ref i);
        protected override void BodyReading(byte[] b, ref int i) => PlayerID = ReadInt(b, ref i);
    }
}