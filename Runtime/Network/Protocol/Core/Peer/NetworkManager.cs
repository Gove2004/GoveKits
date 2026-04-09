// === NetworkManager.cs ===
using Google.Protobuf;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network
{
    public class NetworkManager : MonoSingleton<NetworkManager>
    {
        public Core.NetworkClient Client { get; private set; }
        public Core.NetworkServer Server { get; private set; }

        public enum NetMode { Offline, Client, Host }
        public NetMode Mode { get; private set; } = NetMode.Offline;

        // ========= 启动纯客户端 (连别人) =========
        public void StartClient(string ip, int port)
        {
            Mode = NetMode.Client;
            Client = new Core.NetworkClient();
            
            // 绑定底层的字节消息到你的业务层解析器
            Client.OnMessageReceived += OnClientByteMessageReceived;
            
            Client.Connect(ip, port);
        }

        // ========= 启动主机 (自己当服，又当客) =========
        public void StartHost(int port)
        {
            Mode = NetMode.Host;
            
            Server = new Core.NetworkServer();
            Client = new Core.NetworkClient();

            // 建立魔法桥梁：内存连接
            var localConn = new Core.LocalConnection();
            Server.AddLocalConnection(localConn);
            Client.ConnectLocal(localConn);

            // 绑定事件
            Server.OnMessageReceived += OnServerByteMessageReceived;
            Client.OnMessageReceived += OnClientByteMessageReceived;

            Server.Start(port);
        }

        private void Update()
        {
            // 让内存队列跑起来
            Server?.Update();
            Client?.Update();
        }

        // =================== 业务层对接区 ===================
        // 这里对接你的 MessageDispatcher 和 Protobuf
        
        private void OnClientByteMessageReceived(int msgId, byte[] payload)
        {
            // TODO: 根据 msgId 找到 Protobuf Parser，解析 payload 派发
            // IMessage msg = MessageRegistry.GetParser(msgId).ParseFrom(payload);
            // Dispatcher.DispatchAsync(msg);
        }

        private void OnServerByteMessageReceived(int connId, int msgId, byte[] payload)
        {
            // TODO: 服务端收到消息的逻辑（比如帧同步直接原样 Broadcast，或者状态同步做校验）
            // Server.Broadcast(msgId, payload, excludeId: connId);
        }
        
        // 供业务层调用的发送接口 (比如 NetworkIdentity)
        public void ClientSend(int msgId, Google.Protobuf.IMessage msg)
        {
            byte[] payload = msg.ToByteArray();
            Client.Send(msgId, payload);
        }
    }
}