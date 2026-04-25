using System;
using System.Buffers.Binary;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 代表一个网络会话实体。
    /// 服务端：代表连接上来的客户端；客户端：代表与服务器的连接。
    /// </summary>
    public class Session
    {
        public int SessionId { get; }
        public float RTT { get; set; }
        public bool IsConnected => _connection?.IsConnected ?? false;
        public object UserData { get; set; }
        public event Action<Session, string> OnClosed;

        private readonly IConnection _connection;
        private readonly MessageDispatcher _dispatcher;

        // 核心变更：构造时注入依赖
        public Session(int sessionId, IConnection connection, MessageDispatcher dispatcher)
        {
            SessionId = sessionId;
            _connection = connection;
            _dispatcher = dispatcher;
            
            _connection.OnDataReceived += HandleData;
            _connection.OnDisconnected += HandleDisconnect;
        }

        private void HandleData(IConnection conn, byte[] data)
        {
            try
            {
                if (data.Length < 2) return;

                // 1. 提取 2 字节 Protocol ID
                ushort protocolId = BinaryPrimitives.ReadUInt16LittleEndian(data);
                
                // 2. 提取纯净负载
                var purePayload = new ReadOnlyMemory<byte>(data, 2, data.Length - 2);

                // 3. 反序列化
                var msg = ProtocolCore.Deserialize(protocolId, purePayload);
                if (msg != null)
                {
                    // 4. 分发给业务层
                    _dispatcher.DispatchAsync(this, protocolId, msg).Forget();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Session {SessionId}] Parse Data Error: {ex.Message}");
            }
        }

        public void Send<T>(T message) where T : IProtocolMessage
        {
            if (!IsConnected) return;

            ushort id = ProtocolCore.GetId<T>();
            if (id == 0) return;

            byte[] purePayload = ProtocolCore.Serialize(message);
            
            // 组装 [Id(2字节)] + [Payload(N字节)] 交给连接层 (底层会自动加上4字节长度头)
            byte[] frameData = new byte[2 + purePayload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(frameData, id);
            Buffer.BlockCopy(purePayload, 0, frameData, 2, purePayload.Length);

            _connection.Send(frameData);
        }

        internal void SendRaw(byte[] frameData)
        {
            if (!IsConnected) return;
            _connection.Send(frameData);
        }

        public void Kick(string reason = "") => _connection?.Close(reason);

        private void HandleDisconnect(IConnection conn, string reason)
        {
            _connection.OnDataReceived -= HandleData;
            _connection.OnDisconnected -= HandleDisconnect;
            OnClosed?.Invoke(this, reason);
        }
    }
}