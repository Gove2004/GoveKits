using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    // === 网络模式枚举 ===
    public enum NetworkMode { Offline, Client, Server, Host }

    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        // === 常量配置 ===
        public const int ServerID = 0;           
        public const int HostPlayerID = 1;       
        public const int ClientTempID = -1;      
        public const int BroadcastID = -1;       
        
        public static int NextPlayerID = 100;    
        public const int MaxConnections = 32;

        public string IP = "127.0.0.1";
        public int Port = 12345;

        // === 状态属性 ===
        public NetworkMode Mode { get; private set; } = NetworkMode.Offline;
        public bool IsConnected => _peer != null && _peer.IsAlive;
        
        public bool IsServer => Mode == NetworkMode.Server || Mode == NetworkMode.Host;
        public bool IsClient => Mode == NetworkMode.Client || Mode == NetworkMode.Host;
        public bool IsHost => Mode == NetworkMode.Host;

        // 当前玩家的 ID
        public int MyPlayerID { get; set; } = ClientTempID;

        // === 事件系统 ===
        // 参数 int: 新加入的玩家ID
        public event Action<int> OnClientConnected;
        // 参数 int: 离开的玩家ID
        public event Action<int> OnClientDisconnected;
        // 专门用于客户端：与服务器断开连接
        public event Action OnServerDisconnected; 

        private IPeer _peer;
        private readonly MessageDispatcher _dispatcher = new MessageDispatcher();

        protected void Awake()
        {
            _dispatcher.Bind(this); // 绑定消息处理
            MessageBuilder.AutoRegisterAll();
        }

        // ================== 1. 启动入口 ==================

        public void StartHost()
        {
            StartPeer(NetworkMode.Host);
            MyPlayerID = HostPlayerID;
            // Host 既是Server也是Client，启动即连接成功
            NotifyClientConnected(MyPlayerID); 
        }

        public void StartServer()
        {
            StartPeer(NetworkMode.Server);
            MyPlayerID = ServerID;
        }

        public void StartClient()
        {
            StartPeer(NetworkMode.Client);
            MyPlayerID = ClientTempID; 
            // 注意：客户端此时虽然Socket连上了，但还没有ID，
            // 所以暂时不触发 OnClientConnected，等收到 HelloMessage 再触发
        }

        public void StartOffline()
        {
            StartPeer(NetworkMode.Offline);
            MyPlayerID = 0;
            NotifyClientConnected(0);
        }

        private void StartPeer(NetworkMode mode)
        {
            Close();
            Mode = mode;
            switch (mode)
            {
                case NetworkMode.Offline: _peer = new OfflinePeer(_dispatcher); break;
                case NetworkMode.Client:  _peer = new ClientPeer(_dispatcher); break;
                case NetworkMode.Server:  _peer = new ServerPeer(_dispatcher); break;
                case NetworkMode.Host:    _peer = new HostPeer(_dispatcher); break;
            }
            
            if (mode != NetworkMode.Offline)
                _peer.Start(IP, Port);
        }

        // ================== 2. 系统消息处理 (核心修改) ==================

        /// <summary>
        /// 处理服务器发来的 ID 分配消息
        /// </summary>
        [MessageHandler(Protocol.HelloID)]
        private void OnReceiveIDAssign(HelloMessage msg)
        {
            // 只有纯客户端需要处理这个，Host 已经自己分配了 ID=1
            if (Mode == NetworkMode.Client)
            {
                MyPlayerID = msg.PlayerID;
                Debug.Log($"[Client] ID Assigned by Server: {MyPlayerID}");
                
                // 只有拿到了 ID，才算真正“进入”了游戏，通知 UI
                NotifyClientConnected(MyPlayerID);
            }
        }


        // ================== 对象管理系统 ==================
        private readonly Dictionary<int, NetworkIdentity> _networkObjects = new Dictionary<int, NetworkIdentity>();

        public void RegisterObject(NetworkIdentity identity)
        {
            if (!_networkObjects.ContainsKey(identity.NetID))
            {
                _networkObjects.Add(identity.NetID, identity);
            }
        }

        public void UnregisterObject(NetworkIdentity identity)
        {
            if (_networkObjects.ContainsKey(identity.NetID))
            {
                _networkObjects.Remove(identity.NetID);
            }
        }

        public NetworkIdentity GetObject(int netId)
        {
            _networkObjects.TryGetValue(netId, out var identity);
            return identity;
        }

        // ================== RPC 统一处理入口 ==================
        
        [MessageHandler(Protocol.RpcID)]
        private void OnHandleRPC(RPCMessage msg)
        {
            // 1. 找到目标物体
            var targetObj = GetObject(msg.NetID);
            if (targetObj == null)
            {
                Debug.LogWarning($"[RPC] Target Object not found. NetID: {msg.NetID}");
                return;
            }

            // 2. 如果我是服务器，收到 RPC 后可能需要广播给其他客户端
            // (这里简化处理：所有 RPC 默认在服务器执行后广播给其他人)
            if (IsServer)
            {
                // 执行本地逻辑
                targetObj.InvokeRPCLocal(msg.MethodName, msg.Parameters);

                // 广播给其他人 (排除发送者)
                BroadcastRPC(msg, msg.Header.SenderID);
            }
            // 3. 如果我是客户端，直接执行
            else
            {
                targetObj.InvokeRPCLocal(msg.MethodName, msg.Parameters);
            }
        }

        // 辅助广播 RPC
        public void BroadcastRPC(RPCMessage msg, int excludeId)
        {
            // // 重新打包发送给其他人
            // foreach (var kvp in ((ServerPeer)_peer).GetConnectionsCopy()) // 需在ServerPeer暴露连接列表副本
            // {
            //     if (kvp.Key != excludeId)
            //     {
            //         // 修正 SenderID 为 Server 或者保持原样，视需求而定
            //         // 这里直接转发
            //         _peer.Send(msg); 
            //     }
            // }
        }

        // ================== 3. 发送外观 (Facade) ==================

        public void SendToServer(Message msg) => SendTo(msg, ServerID);
        public void Broadcast(Message msg) => SendTo(msg, BroadcastID);
        public void SendToPlayer(int playerId, Message msg) => SendTo(msg, playerId);

        private void SendTo(Message msg, int targetID)
        {
            if (_peer == null) return;
            msg.Header.SenderID = MyPlayerID;
            msg.Header.TargetID = targetID;
            _peer.Send(msg);
        }

        // ================== 4. 内部回调 ==================

        // 绑定辅助
        public void Bind(object target) => _dispatcher.Bind(target);
        public void Unbind(object target) => _dispatcher.Unbind(target);

        // 供 Peer 调用：通知有客户端连接 (Server端逻辑)
        // 或者 供 Client 端逻辑在收到 HelloMessage 后调用
        public void NotifyClientConnected(int id)
        {
            UniTask.Post(() => OnClientConnected?.Invoke(id));
        }

        // 供 Peer 调用：通知有客户端断开
        public void NotifyClientDisconnected(int id)
        {
            UniTask.Post(() => OnClientDisconnected?.Invoke(id));
        }

        // 供 Peer/Connection 调用：底层连接断开
        public void OnConnectionClose(int connId)
        {
            if (IsServer)
            {
                NotifyClientDisconnected(connId);
            }
            else
            {
                Debug.LogWarning("Disconnected from server.");
                UniTask.Post(() => OnServerDisconnected?.Invoke());
                // 客户端断开后，重置状态
                Close();
            }
        }

        public void Close()
        {
            _peer?.Stop();
            _peer = null;
            Mode = NetworkMode.Offline;
            MyPlayerID = ClientTempID;
        }

        public override void OnDestroy()
        {
            Close();
            base.OnDestroy();
        }
        private void Update() => _peer?.Update();
    }

    // ================== 5. 消息定义 ==================

    [Message(Protocol.HelloID)]
    public class HelloMessage : Message
    {
        public int PlayerID;
        
        public HelloMessage() { }
        public HelloMessage(int id) { PlayerID = id; }
        
        protected override int BodyLength() => 4;
        protected override void BodyWriting(byte[] buffer, ref int index) => WriteInt(buffer, PlayerID, ref index);
        protected override void BodyReading(byte[] buffer, ref int index) => PlayerID = ReadInt(buffer, ref index);
    }
}