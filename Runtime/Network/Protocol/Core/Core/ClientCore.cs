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
            
            var protocolCore = CoreLocator.Protocol; // 通过定位器获取
            var id = protocolCore.GetId<T>();
            
            if (id == 0) return;
            
            var payload = protocolCore.Serialize(message); // 统一序列化
            _channel.Send(id, payload);
        }

        private void OnFrameReceived(int channelId, ushort protocolId, byte[] payload)
        {
            try
            {
                var msg = CoreLocator.Protocol.Deserialize(protocolId, payload);
                if (msg != null)
                {
                    OnMessageReceived?.Invoke(protocolId, msg);
                    
                    // 分发
                    CoreLocator.Dispatcher.DispatchAsync(channelId, protocolId, msg).Forget();
                }
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