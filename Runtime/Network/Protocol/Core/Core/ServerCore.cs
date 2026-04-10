// Server/NetServer.cs
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public class ServerCore : ICore
    {
        public bool IsListening { get; private set; }
        
        public event Action<int> OnClientConnected;
        public event Action<int, string> OnClientDisconnected;
        public event Action<int, ushort, IProtocolMessage> OnMessageReceived;

        private TcpListener _listener;
        private readonly Dictionary<int, INetChannel> _channels = new();
        private readonly IProtocolMessageSerializer _serializer;
        private int _nextChannelId = 1;

        public ServerCore(IProtocolMessageSerializer serializer = null)
        {
            _serializer = serializer ?? new MessagePackSerializerAdapter();
        }

        public void Start(int port)
        {
            Shutdown();
            
            _listener = new TcpListener(System.Net.IPAddress.Any, port);
            _listener.Start();
            IsListening = true;
            
            AcceptLoop().Forget();
            CoreLocator.Log.Success(nameof(ServerCore), $"真实 TCP 监听已启动在端口: {port}");
        }

        private async UniTaskVoid AcceptLoop()
        {
            while (IsListening)
            {
                try
                {
                    var socket = await _listener.AcceptSocketAsync();
                    int id = _nextChannelId++;
                    
                    var channel = new TcpNetChannel(socket, id);
                    channel.OnFrameReceived += (chId, protoId, payload) => 
                        HandleFrame(chId, protoId, payload);
                    channel.OnError += (chId, reason) => 
                        RemoveClient(chId, reason);
                    
                    _channels[id] = channel;
                    OnClientConnected?.Invoke(id);
                }
                catch (Exception ex)
                {
                    if (IsListening) CoreLocator.Log.Error(nameof(ServerCore), $"监听错误: {ex.Message}");
                }
            }
        }

        private void HandleFrame(int channelId, ushort protocolId, byte[] payload)
        {
            var type = ProtocolRegistry.GetType(protocolId);
            if (type == null) return;

            try
            {
                var msg = _serializer.Deserialize(type, payload);
                OnMessageReceived?.Invoke(channelId, protocolId, msg);
                
                // 帧同步：广播给其他人（可选）
                Broadcast(protocolId, payload, channelId);
            }
            catch (Exception ex)
            {
                CoreLocator.Log.Error(nameof(ServerCore), $"处理消息失败: {ex.Message}");
            }
        }

        public void SendTo<T>(int channelId, T message) where T : IProtocolMessage
        {
            if (!_channels.TryGetValue(channelId, out var channel)) return;
            
            var id = ProtocolRegistry.GetId<T>();
            var payload = _serializer.Serialize(message);
            channel.Send(id, payload);
        }

        public void Broadcast(ushort protocolId, byte[] payload, int excludeChannelId = -1)
        {
            foreach (var kvp in _channels)
                if (kvp.Key != excludeChannelId)
                    kvp.Value.Send(protocolId, payload);
        }

        public void Broadcast<T>(T message, int excludeChannelId = -1) where T : IProtocolMessage
        {
            var id = ProtocolRegistry.GetId<T>();
            var payload = _serializer.Serialize(message);
            Broadcast(id, payload, excludeChannelId);
        }

        private void RemoveClient(int channelId, string reason)
        {
            if (_channels.Remove(channelId, out var ch))
            {
                ch.Dispose();
                OnClientDisconnected?.Invoke(channelId, reason);
            }
        }

        public void Shutdown()
        {
            IsListening = false;
            _listener?.Stop();
            foreach (var ch in _channels.Values) ch.Dispose();
            _channels.Clear();
        }

        public void OnShutdown() => Shutdown();
    }
}