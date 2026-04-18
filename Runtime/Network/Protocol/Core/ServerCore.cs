// Server/NetServer.cs
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class ServerCore
    {
        public static bool IsListening { get; private set; }
        
        public static event Action<int> OnClientConnected;
        public static event Action<int, string> OnClientDisconnected;
        public static event Action<int, ushort, IProtocolMessage> OnMessageReceived;

        private static TcpListener _listener;
        private static ServerNetworkProxy _proxy = new();
        private static readonly Dictionary<int, INetChannel> _channels = new();
        private static int _nextChannelId = 1;

        public static void StartServer(int port)
        {
            Shutdown();

            DispatcherCore.Bind(_proxy);
            
            _listener = new TcpListener(System.Net.IPAddress.Any, port);
            _listener.Start();
            IsListening = true;
            
            AcceptLoop().Forget();
            LogCore.Success(nameof(ServerCore), $"真实 TCP 监听已启动在端口: {port}");
        }

        public static async UniTask StartHost(int port)
        {
            StartServer(port);
            await ClientCore.ConnectAsync("127.0.0.1", port);
        }

        private static async UniTaskVoid AcceptLoop()
        {
            while (IsListening)
            {
                try
                {
                    var socket = await _listener.AcceptSocketAsync();
                    int id = _nextChannelId++;
                    
                    var channel = new TcpNetChannel(socket, id);
                    channel.OnDataReceived += (chId, protoId, payload) => 
                        HandleData(chId, protoId, payload);
                    channel.OnError += (chId, reason) => 
                        RemoveClient(chId, reason);
                    
                    _channels[id] = channel;
                    OnClientConnected?.Invoke(id);

                    LogCore.Info(nameof(ServerCore), $"新客户端连接: Channel {id}");
                }
                catch (Exception ex)
                {
                    if (IsListening) LogCore.Error(nameof(ServerCore), $"监听错误: {ex.Message}");
                }
            }
        }

        private static void HandleData(int channelId, ushort protocolId, byte[] payload)
        {
            try
            {
                var msg = ProtocolCore.Deserialize(protocolId, payload);
                OnMessageReceived?.Invoke(channelId, protocolId, msg);
                
                // 帧同步：广播给其他人（可选）
                // Broadcast(protocolId, payload, channelId);

                // 分发
                DispatcherCore.DispatchAsync(channelId, protocolId, msg).Forget();
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(ServerCore), $"处理消息失败: {ex.Message}");
            }
        }

        public static void SendTo<T>(int channelId, T message) where T : IProtocolMessage
        {
            if (!_channels.TryGetValue(channelId, out var channel)) return;
            
            var id = ProtocolCore.GetId<T>();
            var payload = ProtocolCore.Serialize(message);
            channel.Send(id, payload);
        }

        public static void Broadcast(ushort protocolId, byte[] payload, int excludeChannelId = -1)
        {
            foreach (var kvp in _channels)
                if (kvp.Key != excludeChannelId)
                    kvp.Value.Send(protocolId, payload);
        }

        public static void Broadcast<T>(T message, int excludeChannelId = -1) where T : IProtocolMessage
        {
            var id = ProtocolCore.GetId<T>();
            var payload = ProtocolCore.Serialize(message);
            Broadcast(id, payload, excludeChannelId);
        }

        private static void RemoveClient(int channelId, string reason)
        {
            if (_channels.Remove(channelId, out var ch))
            {
                ch.Dispose();
                OnClientDisconnected?.Invoke(channelId, reason);
            }
        }

        public static void Shutdown()
        {
            IsListening = false;
            _listener?.Stop();
            foreach (var ch in _channels.Values) ch.Dispose();
            _channels.Clear();
            DispatcherCore.Unbind(_proxy);
        }





        private class ServerNetworkProxy
        {
            [MessageHandler]
            public void OnHello(int channelId, HiMsg msg)
            {
                SendTo(channelId, new HiMsg(channelId));
            }


            [MessageHandler]
            public void OnBye(int channelId, ByeMsg msg)
            {
                // 断开连接
                if (_channels.TryGetValue(channelId, out var channel))
                {
                    channel.Dispose();
                    _channels.Remove(channelId);
                }
            }
        }
    }
}