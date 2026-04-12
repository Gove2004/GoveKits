// Client/NetClient.cs
using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class ClientCore
    {
        public static bool IsConnected => _channel?.IsActive ?? false;
        
        public static event Action OnConnected;
        public static event Action<string> OnDisconnected;
        public static event Action<ushort, IProtocolMessage> OnMessageReceived;

        private static INetChannel _channel;

        public static async UniTask ConnectAsync(string host, int port)
        {
            Shutdown();

            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(host, port);
                
                _channel = new TcpNetChannel(socket, 0);
                _channel.OnFrameReceived += OnFrameReceived;
                _channel.OnError += (id, reason) => 
                {
                    OnDisconnected?.Invoke(reason);
                };
                
                OnConnected?.Invoke();
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

        private static void OnFrameReceived(int channelId, ushort protocolId, byte[] payload)
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
        }
    }
}