using System;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    public class LocalConnection : IConnection
    {
        public bool IsConnected { get; private set; }
        public EndPoint RemoteEndPoint { get; } = new IPEndPoint(IPAddress.Loopback, 0);

        public event Action<IConnection> OnConnected;
        public event Action<IConnection, string> OnDisconnected;
        public event Action<IConnection, byte[]> OnFrameReceived;

        private LocalConnection _peer;
        
        private LocalConnection() { }

        public static (LocalConnection client, LocalConnection server) CreatePair()
        {
            var client = new LocalConnection();
            var server = new LocalConnection();
            client._peer = server;
            server._peer = client;
            client.IsConnected = true;
            server.IsConnected = true;
            return (client, server);
        }

        public Task<bool> ConnectAsync(EndPoint target)
        {
            OnConnected?.Invoke(this);
            return Task.FromResult(true);
        }

        public void SendFrame(byte[] frameData)
        {
            if (!IsConnected || _peer == null) return;

            byte[] copy = new byte[frameData.Length];
            Buffer.BlockCopy(frameData, 0, copy, 0, frameData.Length);

            UniTask.Post(() =>
            {
                if (_peer.IsConnected)
                {
                    _peer.OnFrameReceived?.Invoke(_peer, copy);
                }
            });
        }

        public void Close(string reason = "")
        {
            if (!IsConnected) return;
            IsConnected = false;
            OnDisconnected?.Invoke(this, reason);
            if (_peer != null && _peer.IsConnected) _peer.Close("Peer closed");
        }

        public void Dispose() => Close("Dispose");
        public void SimulateConnected() => OnConnected?.Invoke(this);
    }
}