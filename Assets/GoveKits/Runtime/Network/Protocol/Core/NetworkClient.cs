using System;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Network.Protocol
{
    public enum ClientState { Disconnected, Connecting, Connected }

    public class NetworkClient
    {
        public ClientState State { get; private set; } = ClientState.Disconnected;
        public bool IsConnected => State == ClientState.Connected;

        public event Action OnConnected;
        public event Action OnDisconnected;

        private TcpTransport _transport;
        private PacketParser _parser;
        private readonly MessageDispatcher _dispatcher;

        public NetworkClient(MessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _parser = new PacketParser(OnMessageDecoded);
            _transport = new TcpTransport();
            
            _transport.OnReceive = _parser.Input;
            _transport.OnDisconnected = HandleDisconnect;
        }

        public void Connect(string ip, int port)
        {
            if (State != ClientState.Disconnected) return;
            LogCore.Log("Client", $"Connecting to {ip}:{port}...");
            State = ClientState.Connecting;

            ConnectAsync(ip, port).Forget();
        }

        private async UniTaskVoid ConnectAsync(string ip, int port)
        {
            try
            {
                // 等待底层 Transport 连接成功 (或抛出异常)
                await _transport.ConnectAsync(ip, port);
                
                // 成功
                State = ClientState.Connected;
                LogCore.Log("Client", "Connected!");
                OnConnected?.Invoke();
            }
            catch (Exception)
            {
                LogCore.LogError("Client", "Connection Failed/Timeout");
                // 确保重置状态
                HandleDisconnect();
            }
        }

        public void Send(IMessage msg)
        {
            if (!IsConnected) return;
            byte[] data = PacketParser.Pack(msg, out int len);
            if (data != null)
            {
                _transport.Send(data, len);
                BufferPool.Return(data);
            }
        }

        public void Disconnect() => _transport.Close();

        private void HandleDisconnect()
        {
            if (State == ClientState.Disconnected) return;
            State = ClientState.Disconnected;
            LogCore.Log("Client", "Disconnected");
            OnDisconnected?.Invoke();
        }

        private void OnMessageDecoded(IMessage msg)
        {
            _dispatcher.DispatchAsync(msg).Forget();
        }
    }
}