using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
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

            Debug.Log($"[Network] Connecting to {ip}:{port}...");
            State = ClientState.Connecting;

            _transport.Connect(ip, port);
            
            CheckConnectionStatus().Forget();
        }

        private async UniTaskVoid CheckConnectionStatus()
        {
            // 5秒超时检测
            float timeout = 5f;
            while (timeout > 0)
            {
                if (_transport.IsConnected)
                {
                    State = ClientState.Connected;
                    Debug.Log("[Network] Connected!");
                    OnConnected?.Invoke();
                    return;
                }
                await UniTask.Delay(100);
                timeout -= 0.1f;
            }

            if (State == ClientState.Connecting)
            {
                Debug.LogError("[Network] Connection Timeout");
                Disconnect();
            }
        }

        public void Send(Message msg)
        {
            if (!IsConnected) return;
            
            // 无Header，直接打包 Body
            byte[] data = PacketParser.Pack(msg, out int len);
            _transport.Send(data, len);
        }

        public void Disconnect()
        {
            _transport.Close();
        }

        private void HandleDisconnect()
        {
            if (State == ClientState.Disconnected) return;
            State = ClientState.Disconnected;
            Debug.Log("[Network] Disconnected");
            OnDisconnected?.Invoke();
        }

        private void OnMessageDecoded(Message msg)
        {
            _dispatcher.DispatchAsync(msg).Forget();
        }
    }
}