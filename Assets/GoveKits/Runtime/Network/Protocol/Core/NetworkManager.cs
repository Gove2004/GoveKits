using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    // === 网络模式枚举 ===
    public enum NetworkMode { Offline, Client, Host }

    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        // === 常量配置 ===
        public const int ServerID = 0;           // 服务器逻辑ID
        public const int HostPlayerID = 1;       // Host 玩家自身的ID
        public const int ClientTempID = -1;      // 客户端临时ID
        public const int BroadcastID = -1;       // 广播目标ID
        
        public static int NextPlayerID = 100;    // 外部客户端起始ID
        public const int MaxConnections = 32;

        [Header("Settings")]
        public string IP = "127.0.0.1";
        public int Port = 12345;
        [Tooltip("是否启动时自动作为客户端连接服务器")]
        public bool AutoConnect = false;

        // === 状态属性 ===
        public NetworkMode Mode { get; private set; } = NetworkMode.Offline;
        public bool IsConnected => _peer != null && _peer.IsAlive;
        
        public bool IsHost => Mode == NetworkMode.Host;
        public bool IsClient => Mode == NetworkMode.Client || Mode == NetworkMode.Host; 
        
        // Host 拥有服务器权限
        public bool IsServer => Mode == NetworkMode.Host; 
        
        // 当前玩家的 ID
        public int MyPlayerID = ClientTempID;

        // === 事件系统 ===
        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;
        public event Action OnServerDisconnected; 

        // === 核心组件 ===
        private IPeer _peer;
        private readonly MessageDispatcher _dispatcher = new MessageDispatcher();
        
        // 对象管理 (NetID -> Identity)
        private readonly Dictionary<int, NetworkIdentity> _networkObjects = new Dictionary<int, NetworkIdentity>();

        protected void Awake()
        {
            _dispatcher.Bind(this); // 绑定自身消息
            
            MessageBuilder.Register(typeof(HelloMessage), Protocol.HelloID);
            MessageBuilder.AutoRegisterAll();

            if (AutoConnect) StartClient();
        }

        // ================== 1. 启动入口 ==================

        public void StartHost()
        {
            StartPeer(NetworkMode.Host);
            MyPlayerID = HostPlayerID;
            // Host 启动即连接成功 (本地回环)
            NotifyClientConnected(MyPlayerID); 
        }

        public void StartClient()
        {
            StartPeer(NetworkMode.Client);
            MyPlayerID = ClientTempID; 
            // Socket连接建立，等待 HelloMessage
        }

        public void StartOffline()
        {
            StartPeer(NetworkMode.Offline);
            MyPlayerID = 0;
            NotifyClientConnected(0);
        }

        private void StartPeer(NetworkMode mode)
        {
            Close(); // 清理旧状态
            Mode = mode;
            switch (mode)
            {
                case NetworkMode.Offline: _peer = new OfflinePeer(_dispatcher); break;
                case NetworkMode.Client:  _peer = new ClientPeer(_dispatcher); break;
                case NetworkMode.Host:    _peer = new HostPeer(_dispatcher); break;
            }
            
            if (mode != NetworkMode.Offline)
                _peer.Start(IP, Port);
        }

        // ================== 2. 连接管理与握手 ==================

        /// <summary>
        /// 供 Peer 调用：通知有底层连接建立
        /// </summary>
        public void NotifyClientConnected(int id)
        {
            // 如果我是 Host，并且连进来的不是我自己 (即外部 TCP 客户端)
            if (IsHost && id != MyPlayerID)
            {
                Debug.Log($"[Host] TCP Client {id} Connected. Sending Hello...");
                // 主动发送握手包，分配 ID
                SendToPlayer(id, new HelloMessage(id));
            }

            // 触发事件
            UniTask.Post(() => OnClientConnected?.Invoke(id));
        }

        /// <summary>
        /// 客户端处理：收到服务器分配的 ID
        /// </summary>
        [MessageHandler(Protocol.HelloID)]
        private void OnReceiveIDAssign(HelloMessage msg)
        {
            if (Mode == NetworkMode.Client)
            {
                MyPlayerID = msg.PlayerID;
                Debug.Log($"[Client] Handshake Complete. Assigned ID: {MyPlayerID}");
                NotifyClientConnected(MyPlayerID);
            }
        }

        public void NotifyClientDisconnected(int id)
        {
            UniTask.Post(() => OnClientDisconnected?.Invoke(id));
        }

        public void OnConnectionClose(int connId)
        {
            if (IsHost)
            {
                NotifyClientDisconnected(connId);
            }
            else
            {
                Debug.LogWarning("Disconnected from server.");
                UniTask.Post(() => OnServerDisconnected?.Invoke());
                Close();
            }
        }

        // ================== 3. RPC 路由逻辑 ==================
        
        [MessageHandler(Protocol.RpcID)]
        private void OnHandleRPC(RPCMessage msg)
        {
            var targetObj = GetObject(msg.NetID);
            if (targetObj == null) return;

            // A. Host 逻辑 (权威方)
            if (IsHost)
            {
                // 1. 执行本地逻辑 (更新 Host 自己的游戏世界)
                targetObj.InvokeRPCLocal(msg.MethodName, msg.Parameters);

                // 2. 广播给其他 TCP 客户端 (排除发送者)
                BroadcastRPC(msg, msg.Header.SenderID);
            }
            // B. Client 逻辑
            else
            {
                // 客户端只执行，不转发
                targetObj.InvokeRPCLocal(msg.MethodName, msg.Parameters);
            }
        }

        /// <summary>
        /// 辅助广播 RPC (仅 Host 可用)
        /// </summary>
        public void BroadcastRPC(RPCMessage msg, int excludeId)
        {
            if (_peer is HostPeer hostPeer)
            {
                var connections = hostPeer.GetConnectionsCopy();
                
                foreach (var kvp in connections)
                {
                    int connId = kvp.Key;
                    
                    // 1. 排除 excludeId (触发者)
                    // 2. 排除 HostPlayerID (Host 本地已执行，防止回环重复执行)
                    if (connId != excludeId && connId != HostPlayerID)
                    {
                        kvp.Value.Send(msg);
                    }
                }
            }
        }

        // ================== 4. 外观发送接口 ==================

        /// <summary>
        /// 发送给服务器 (Client/Host通用)
        /// </summary>
        public void SendToServer(Message msg) => SendTo(msg, ServerID);
        
        /// <summary>
        /// 广播 (仅 Host 可用)
        /// </summary>
        public void Broadcast(Message msg) => SendTo(msg, BroadcastID);
        
        /// <summary>
        /// 发送给特定玩家 (仅 Host 可用)
        /// </summary>
        public void SendToPlayer(int playerId, Message msg) => SendTo(msg, playerId);

        private void SendTo(Message msg, int targetID)
        {
            if (_peer == null) return;
            msg.Header.SenderID = MyPlayerID;
            msg.Header.TargetID = targetID;
            _peer.Send(msg);
        }

        // ================== 5. 对象与辅助 ==================
        
        public void RegisterObject(NetworkIdentity identity)
        {
            if (!_networkObjects.ContainsKey(identity.NetID))
                _networkObjects.Add(identity.NetID, identity);
        }

        public void UnregisterObject(NetworkIdentity identity)
        {
            if (_networkObjects.ContainsKey(identity.NetID))
                _networkObjects.Remove(identity.NetID);
        }

        public NetworkIdentity GetObject(int netId)
        {
            _networkObjects.TryGetValue(netId, out var identity);
            return identity;
        }

        public void Bind(object target) => _dispatcher.Bind(target);
        public void Unbind(object target) => _dispatcher.Unbind(target);

        public void Close()
        {
            var prevMode = Mode;
            Mode = NetworkMode.Offline; 

            if (_peer != null)
            {
                _peer.Stop();
                _peer = null;
            }

            MyPlayerID = ClientTempID;
            _networkObjects.Clear();
            
            if (prevMode != NetworkMode.Offline)
            {
                Debug.Log("[NetworkManager] Closed.");
            }
        }

        public override void OnDestroy()
        {
            Close();
            base.OnDestroy();
        }

        private void Update() => _peer?.Update();
    }
}