// Client/NetClient.cs
using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class ClientCore
    {
        public static int PlayerId { get; private set; }
        public static bool IsConnected => _channel?.IsActive ?? false;
        
        public static event Action OnConnected;
        public static event Action<string> OnDisconnected;
        public static event Action<ushort, IProtocolMessage> OnMessageReceived;

        private static INetChannel _channel;
        private static ClientNetworkProxy _proxy = new();

        public static async UniTask ConnectAsync(string host, int port)
        {
            Shutdown();

            DispatcherCore.Bind(_proxy);

            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(host, port);
                
                _channel = new TcpNetChannel(socket, 0);
                _channel.OnDataReceived += OnDataReceived;
                _channel.OnError += (id, reason) => 
                {
                    OnDisconnected?.Invoke(reason);
                };
                
                OnConnected?.Invoke();

                Send(new HiMsg(0));

                LogCore.Success(nameof(ClientCore), $"服务器已连接 {host}:{port}");
            }
            catch (Exception ex)
            {
                OnDisconnected?.Invoke($"Connect failed: {ex.Message}");
            }
        }

        public static void Send<T>(T message) where T : IProtocolMessage
        {
            if (!IsConnected) return;
            
            var id = ProtocolCore.GetId<T>();
            
            if (id == 0) return;
            
            var payload = ProtocolCore.Serialize(message); // 统一序列化
            _channel.Send(id, payload);
        }

        private static void OnDataReceived(int channelId, ushort protocolId, byte[] payload)
        {
            try
            {
                var msg = ProtocolCore.Deserialize(protocolId, payload);

                if (msg != null)
                {
                    OnMessageReceived?.Invoke(protocolId, msg);
                    
                    // 分发
                    DispatcherCore.DispatchAsync(channelId, protocolId, msg).Forget();
                }
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(ClientCore), $"处理消息失败: {ex.Message}");
            }
        }


        public static void Shutdown()
        {
            _channel?.Dispose();
            _channel = null;
            PlayerId = 0;
            DispatcherCore.Unbind(_proxy);
        }

        

        private class ClientNetworkProxy
        {
            [MessageHandler]
            public void OnHello(HiMsg msg)
            {
                // 由于Host同时运行Client和Server，导致Client首次发送的包也会被收到，因此需要舍弃
                if (msg.Int == 0) return;

                PlayerId = msg.Int;  // 服务器分配的玩家ID
                _channel.SetID(PlayerId); // 设置连接通道ID为玩家ID

                LogCore.Info(nameof(ClientNetworkProxy), $"玩家ID已设置: {PlayerId}");
            }


            [MessageHandler]
            public void OnBye(ByeMsg msg)
            {
                Shutdown();

                LogCore.Info(nameof(ClientNetworkProxy), $"收到 Bye 消息: {msg.Str}");
            }
        }
    }
}