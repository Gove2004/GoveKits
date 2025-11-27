using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public enum NetworkMode { Offline, Client, Host }

    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        // === 常量配置 ===

        public const int OfflineID = -1;   
        public const int ServerID = 0;           
        public const int HostPlayerID = 1;   
        public static int NextPlayerID = 100;     
        public const int BroadcastID = -1;       
        public const int MaxConnections = 32;    

        [Header("Settings")]
        public string IP = "127.0.0.1";
        public int Port = 12345;
        public bool AutoConnect = false;

        // === 状态属性 ===
        public NetworkMode Mode { get; private set; } = NetworkMode.Offline;
        public bool IsConnected => _peer != null && _peer.IsAlive;
        
        public bool IsHost => Mode == NetworkMode.Host;
        public bool IsClient => Mode == NetworkMode.Client; 
        
        // 当前实例代表的玩家ID (Server端逻辑无所谓这个值，但Host作为玩家时是1)
        public int MyPlayerID = OfflineID;

        // === 事件系统 ===
        public event Action<int> OnClientConnectedEvent;
        public event Action<int> OnClientDisconnectedEvent;
        public event Action OnServerConnectedEvent;
        public event Action OnServerDisconnectedEvent; 


        private IPeer _peer;
        private readonly MessageDispatcher _dispatcher = new MessageDispatcher();

        protected void Awake()
        {
            _dispatcher.Bind(this); 
            
            MessageBuilder.AutoRegisterAll();

            if (AutoConnect) StartClient();
        }

        // ================== 1. 启动入口 ==================

        public void StartHost()
        {
            MyPlayerID = OfflineID;
            StartPeer(NetworkMode.Host);
        }

        public void StartClient()
        {
            MyPlayerID = OfflineID; // 等待服务器分配
            StartPeer(NetworkMode.Client);
        }

        public void StartOffline()
        {
            MyPlayerID = OfflineID;
            StartPeer(NetworkMode.Offline);
        }

        private void StartPeer(NetworkMode mode)
        {
            Close();
            Mode = mode;
            switch (mode)
            {
                case NetworkMode.Offline: _peer = new OfflinePeer(_dispatcher); break;
                case NetworkMode.Client:  _peer = new ClientPeer(_dispatcher); break;
                case NetworkMode.Host:    _peer = new HostPeer(_dispatcher); break;
            }
            
            if (_peer is ClientPeer clientPeer)
            {
                clientPeer.OnServerConnectedEvent += OnServerConnected;
                clientPeer.OnServerDisconnectedEvent += OnServerDisconnected;
            }
            else if (_peer is HostPeer hostPeer)
            {
                hostPeer.OnClientConnectedEvent += OnClientConnected;
                hostPeer.OnClientDisconnectedEvent += OnClientDisconnected;
                hostPeer.OnServerConnectedEvent += OnServerConnected;
                hostPeer.OnServerDisconnectedEvent += OnServerDisconnected;
            }

            if (mode != NetworkMode.Offline) _peer.Start(IP, Port);
        }

        // ================== 2. 外观发送接口 (智能路由) ==================

        /// <summary>
        /// 发送给服务器 (Target = 0)
        /// </summary>
        public void SendToServer(Message msg) => SendToPlayer(ServerID, msg);

        /// <summary>
        /// 发送给指定玩家 (自动处理中转)
        /// </summary>
        public void SendToPlayer(int targetId, Message msg)
        {
            // 1. 填充 Header
            msg.Header.SenderID = MyPlayerID;
            msg.Header.TargetID = targetId;
            
            // 2. 路由策略
            if (IsHost)
            {
                // Host 是上帝，拥有所有连接，直接发
                // HostPeer 内部会处理：如果是发给Server(0)走回环，发给Client走TCP
                _peer.Send(msg);
            }
            else
            {
                // Client 视角
                if (targetId == ServerID)
                {
                    // 直连服务器
                    _peer.Send(msg);
                }
                else
                {
                    // 发给其他玩家 -> 请求服务器 Relay
                    // 外层包是发给 Server 的
                    var relay = new RelayMessage(targetId, msg);
                    relay.Header.SenderID = MyPlayerID;
                    relay.Header.TargetID = ServerID; 
                    _peer.Send(relay);
                }
            }
        }

        /// <summary>
        /// 广播 (默认排除自己) 默认-1 表示不排除任何人, 0 表示排除自己
        /// </summary>
        public void Broadcast(Message msg, int excludeClientId = -1)
        {
            if (excludeClientId == 0) excludeClientId = MyPlayerID;
            
            msg.Header.SenderID = MyPlayerID;
            msg.Header.TargetID = BroadcastID;

            if (IsHost)
            {
                // Host 直接向所有连接分发
                if (_peer is HostPeer hostPeer)
                {
                    hostPeer.SendToAll(msg, excludeClientId);
                }
            }
            else
            {
                // Client 请求服务器 Relay 广播
                var relay = new RelayMessage(BroadcastID, msg, new int[] { excludeClientId });
                relay.Header.SenderID = MyPlayerID;
                relay.Header.TargetID = ServerID;
                _peer.Send(relay);
            }
        }

        // ================== 3. 服务器中转逻辑 (核心) ==================

        [MessageHandler(Protocol.RelayID)]
        private void OnHandleRelay(RelayMessage relayMsg)
        {
            // 只有 Server 权限才能执行转发
            if (!IsHost) return;

            // 1. 还原内部消息
            Message innerMsg = relayMsg.GetMessage<Message>();
            if (innerMsg == null) return;

            // 2. 安全校验：强制修正 Sender 为实际发包者 (防止 Client A 冒充 Client B)
            innerMsg.Header.SenderID = relayMsg.Header.SenderID; 
            innerMsg.Header.TargetID = relayMsg.targetId;

            // Debug.Log($"[Server Relay] {innerMsg.MsgID} From {innerMsg.Header.SenderID} To {relayMsg.targetId}");

            // 3. 执行分发
            if (_peer is HostPeer hostPeer)
            {
                if (relayMsg.targetId == BroadcastID)
                {
                    // 广播模式：处理排除列表
                    var excludes = relayMsg.ExcludeIDs != null
                        ? new HashSet<int>(relayMsg.ExcludeIDs)
                        : new HashSet<int>();
                    if (relayMsg.ExcludeIDs != null)
                    {
                        foreach (var id in relayMsg.ExcludeIDs) excludes.Add(id);
                    }
                    
                    hostPeer.SendToAll(innerMsg, excludes);
                }
                else
                {
                    // 单发模式
                    hostPeer.Send(innerMsg);
                }
            }
        }

        // ================== 4. RPC 处理 ==================
        
        [MessageHandler(Protocol.RpcID)]
        private void OnHandleRPC(RPCMessage msg)
        {
            // 现在向 SpawnerManager 查询物体
            if (SpawnerManager.Instance == null) return;
            var targetObj = SpawnerManager.Instance.GetObject(msg.NetID);

            // RPC 的执行逻辑：收到消息意味着“有人让我执行”。
            targetObj.InvokeRPCLocal(msg.MethodName, msg.Parameters);

             // 如果我是 Host，我还肩负着“转发给其他人”的责任（如果是广播RPC）。
            if (IsHost)
            {
                // 如果是广播 RPC，Server 收到后需要转发给其他人
                var excludes = new HashSet<int>();
                // excludes.Add(msg.Header.SenderID); // 排除发送者
                excludes.Add(HostPlayerID);        // 排除 Host 自己 (因为上面Invoke过了)
                
                if (_peer is HostPeer hostPeer)
                {
                    hostPeer.SendToAll(msg, excludes);
                }
            }
        }

        // ================== 5. 连接管理 ==================

        public void KickPlayer(int id)
        {
            if (IsHost && _peer is HostPeer hostPeer)
            {
                hostPeer.KickClient(id);
            }
        }


        public void SendIDAssign(int clientId)
        {
            if (IsHost)
            {
                var hello = new HelloMessage(clientId);
                SendToPlayer(clientId, hello);
            }
        }


        [MessageHandler(Protocol.HelloID)]
        private void OnReceiveIDAssign(HelloMessage msg)
        {
            MyPlayerID = msg.PlayerID;
            Debug.Log($"[Client] Handshake Complete. Assigned ID: {MyPlayerID}");
        }


        private void OnServerConnected()
        {
            Debug.Log("[Client] Connected to Server.");

            OnServerConnectedEvent?.Invoke();
        }

        private void OnServerDisconnected()
        {
            Debug.LogWarning("[Client] Disconnected from Server.");

            OnServerDisconnectedEvent?.Invoke();
        }

        private void OnClientConnected(int clientId)
        {
            Debug.Log($"[Server] Client {clientId} Connected.");
            SendIDAssign(clientId);

            OnClientConnectedEvent?.Invoke(clientId);
        }

        private void OnClientDisconnected(int clientId)
        {
            Debug.Log($"[Server] Client {clientId} Disconnected.");

            OnClientDisconnectedEvent?.Invoke(clientId);
        }


        // ================== 6. 基础功能 ==================
        public void Bind(object target) => _dispatcher.Bind(target);
        public void Unbind(object target) => _dispatcher.Unbind(target);

        public void Close()
        {
            var prevMode = Mode;
            Mode = NetworkMode.Offline;

            if (_peer is ClientPeer clientPeer)
            {
                clientPeer.OnServerConnectedEvent -= OnServerConnected;
                clientPeer.OnServerDisconnectedEvent -= OnServerDisconnected;
            }
            else if (_peer is HostPeer hostPeer)
            {
                hostPeer.OnServerConnectedEvent -= OnServerConnected;
                hostPeer.OnServerDisconnectedEvent -= OnServerDisconnected;
                hostPeer.OnClientConnectedEvent -= OnClientConnected;
                hostPeer.OnClientDisconnectedEvent -= OnClientDisconnected;
            }
            _peer?.Stop();
            _peer = null;

            MyPlayerID = OfflineID;

            if (prevMode != NetworkMode.Offline) Debug.Log("[NetworkManager] Closed.");
        }

        public override void OnDestroy() { Close(); base.OnDestroy(); }
        private void Update() => _peer?.Update();
    }
}