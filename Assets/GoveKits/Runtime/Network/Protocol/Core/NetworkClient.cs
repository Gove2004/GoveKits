using System;
using Cysharp.Threading.Tasks;
using Google.Protobuf;


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
            LogManager.Log("Client", $"Connecting to {ip}:{port}...");
            State = ClientState.Connecting;

            _transport.Connect(ip, port);
            // 简单的状态检查 (实际项目可加 Timer)
            CheckStatus().Forget();
        }

        private async UniTaskVoid CheckStatus()
        {
            await UniTask.Delay(5000);
            if (!IsConnected && State == ClientState.Connecting) 
            {
                LogManager.LogWarning("Client", "Connection timed out.");
                Disconnect();
            }
        }

        public void Send(IMessage msg)
        {
            if (!IsConnected) return;
            
            // 使用 BufferPool 的 byte[]
            byte[] data = PacketParser.Pack(msg, out int len);
            if (data != null)
            {
                _transport.Send(data, len);
                
                // 记得归还给 Pool
                BufferPool.Return(data); 
            }
        }

        public void Disconnect()
        {
            _transport.Close();
        }

        private void HandleDisconnect()
        {
            State = ClientState.Disconnected;
            OnDisconnected?.Invoke();
        }

        private void OnMessageDecoded(IMessage msg)
        {
            _dispatcher.DispatchAsync(msg).Forget();
        }
    }
}