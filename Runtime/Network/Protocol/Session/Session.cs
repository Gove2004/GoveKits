using System;
using System.Buffers.Binary;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public class Session
    {
        public int SessionId { get; }
        public float RTT { get; set; }
        public bool IsConnected => _connection?.IsConnected ?? false;
        public object UserData { get; set; }
        public event Action<Session, string> OnClosed;

        private readonly IConnection _connection;

        internal Session(int sessionId, IConnection connection)
        {
            SessionId = sessionId;
            _connection = connection;
            _connection.OnFrameReceived += HandleFrameData;
            _connection.OnDisconnected += HandleDisconnect;
        }

        private void HandleFrameData(IConnection conn, byte[] frameData)
        {
            try
            {
                if (frameData.Length < 2) return;

                // 1. 提取 2 字节 ID
                ushort protocolId = BinaryPrimitives.ReadUInt16LittleEndian(frameData);
                
                // 2. 提取纯净负载
                var purePayload = new ReadOnlyMemory<byte>(frameData, 2, frameData.Length - 2);

                // 3. 反序列化并分发
                var msg = ProtocolCore.Deserialize(protocolId, purePayload);
                if (msg != null)
                {
                    DispatcherCore.DispatchAsync(this, protocolId, msg).Forget();
                }
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(Session), $"解析数据异常 SessionId:{SessionId}, {ex.Message}");
            }
        }

        public void Send<T>(T message) where T : IProtocolMessage
        {
            if (!IsConnected) return;

            ushort id = ProtocolCore.GetId<T>();
            if (id == 0) return;

            byte[] purePayload = ProtocolCore.Serialize(message);
            
            // 组装 [Id(2)] + [Payload(N)] 交给连接层 (连接层会自动加 4字节长度)
            byte[] frameData = new byte[2 + purePayload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(frameData, id);
            Buffer.BlockCopy(purePayload, 0, frameData, 2, purePayload.Length);

            _connection.SendFrame(frameData);
        }

        internal void SendRaw(byte[] frameData)
        {
            if (!IsConnected) return;
            _connection.SendFrame(frameData);
        }

        public void Kick(string reason = "") => _connection?.Close(reason);

        private void HandleDisconnect(IConnection conn, string reason)
        {
            _connection.OnFrameReceived -= HandleFrameData;
            _connection.OnDisconnected -= HandleDisconnect;
            OnClosed?.Invoke(this, reason);
        }
    }
}