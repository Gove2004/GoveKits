// === NetworkConnection.cs ===
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GoveKits.Network
{
     // === 逻辑连接对象 ===
    public class NetworkConnection
    {
        public int ConnectionId { get; private set; }
        public bool IsAlive => _transport != null && _transport.IsConnected;
        public ITransport Transport => _transport; // 暴露给 HostPeer 用于 Link

        private ITransport _transport;
        private PacketParser _parser;
        private MessageDispatcher _dispatcher;

        // 对外事件
        public event Action OnDisconnected;

        public NetworkConnection(int id, ITransport transport, MessageDispatcher dispatcher)
        {
            ConnectionId = id;
            _transport = transport;
            _dispatcher = dispatcher;

            _parser = new PacketParser(OnMessageParsed);

            _transport.OnReceiveData = OnDataReceived;
            _transport.OnDisconnected = OnTransportDisconnected;
        }

        private void OnDataReceived(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            _parser.InputRawData(data, 0, data.Length);
        }

        private async UniTask OnMessageParsed(Message msg)
        {
            // 接收时，强制标记来源ID，防止伪造
            msg.Header.SenderID = ConnectionId;
            await _dispatcher.DispatchAsync(msg);
        }

        public void Send(Message msg)
        {
            if (!IsAlive) return;
            byte[] bytes = PacketParser.PackMessage(msg);
            _transport.Send(bytes);
        }

        private void OnTransportDisconnected()
        {
            OnDisconnected?.Invoke();
        }

        public void Close()
        {
            _transport?.Close();
            _transport = null;
        }
    }
}