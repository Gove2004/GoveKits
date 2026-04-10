// Client/NetClient.cs
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network
{
    public class ClientCore : ICore
    {
        public bool IsConnected => _channel?.IsActive ?? false;
        
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<ushort, IProtocolMessage> OnMessageReceived;

        private INetChannel _channel;
        private readonly IProtocolMessageSerializer _serializer;

        public ClientCore(IProtocolMessageSerializer serializer = null)
        {
            _serializer = serializer ?? new MessagePackSerializerAdapter();
        }

        public async UniTask ConnectAsync(string host, int port)
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
                    OnShutdown();
                };
                
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnDisconnected?.Invoke($"Connect failed: {ex.Message}");
            }
        }

        public void Send<T>(T message) where T : IProtocolMessage
        {
            if (!IsConnected) return;
            
            var id = ProtocolRegistry.GetId<T>();
            if (id == 0)
            {
                CoreLocator.Log.Error(nameof(ClientCore), $"未注册的消息类型: {typeof(T).Name}");
                return;
            }
            
            var payload = _serializer.Serialize(message);
            _channel.Send(id, payload);
        }

        private void OnFrameReceived(int channelId, ushort protocolId, byte[] payload)
        {
            var type = ProtocolRegistry.GetType(protocolId);
            if (type == null)
            {
                CoreLocator.Log.Warn(nameof(ClientCore), $"未注册的消息类型: {protocolId}");
                return;
            }

            try
            {
                var msg = _serializer.Deserialize(type, payload);
                OnMessageReceived?.Invoke(protocolId, msg);
            }
            catch (Exception ex)
            {
                CoreLocator.Log.Error(nameof(ClientCore), $"处理消息失败: {ex.Message}");
            }
        }


        public void Shutdown()
        {
            _channel?.Dispose();
            _channel = null;
        }

        public void OnShutdown() => Shutdown();
    }
}