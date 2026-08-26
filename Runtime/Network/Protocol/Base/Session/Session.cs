using System;
using System.Buffers.Binary;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 网络会话实体，包装 IChannel 并提供协议层面的收发。
    /// 服务端：代表连接上来的客户端；客户端：代表与服务器的连接。
    /// </summary>
    public class Session
    {
        /// <summary>会话唯一标识 ID。</summary>
        public int SessionId { get; }
        /// <summary>往返延迟（毫秒），由 Ping 机制测量。</summary>
        public float RTT { get; set; }
        /// <summary>当前连接是否处于已连接状态。</summary>
        public bool IsConnected => _channel?.IsConnected ?? false;
        /// <summary>用户自定义附加数据，可用于存储业务相关状态。</summary>
        public object UserData { get; set; }
        /// <summary>会话关闭时触发，携带会话引用和关闭原因。</summary>
        public event Action<Session, string> OnClosed;

        private readonly IChannel _channel;
        private readonly MessageDispatcher _dispatcher;
        private readonly ProtocolCenter _protocol;

        /// <summary>
        /// 创建新的网络会话。
        /// </summary>
        /// <param name="sessionId">会话 ID。</param>
        /// <param name="channel">底层通信通道。</param>
        /// <param name="dispatcher">消息分发器。</param>
        /// <param name="protocol">协议中心，用于序列化和协议 ID 查找。</param>
        public Session(int sessionId, IChannel channel, MessageDispatcher dispatcher, ProtocolCenter protocol)
        {
            SessionId = sessionId;
            _channel = channel;
            _dispatcher = dispatcher;
            _protocol = protocol;

            _channel.OnDataReceived += HandleData;
            _channel.OnDisconnected += HandleDisconnect;
        }

        private void HandleData(IChannel conn, byte[] data)
        {
            try
            {
                if (data.Length < 2) return;

                ushort protocolId = BinaryPrimitives.ReadUInt16LittleEndian(data);
                var purePayload = new ReadOnlyMemory<byte>(data, 2, data.Length - 2);

                var msg = _protocol.Deserialize(protocolId, purePayload);
                if (msg != null)
                {
                    _dispatcher.DispatchAsync(this, protocolId, msg).Forget();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Session {SessionId}] 解析数据错误: {ex.Message}");
            }
        }

        /// <summary>发送协议消息到对端。</summary>
        /// <param name="message">待发送的消息实例。</param>
        public void Send<T>(T message) where T : IProtocolMessage
        {
            if (!IsConnected) return;

            ushort id = _protocol.GetId<T>();
            if (id == 0) return;

            byte[] purePayload = _protocol.Serialize(message);

            byte[] frameData = new byte[2 + purePayload.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(frameData, id);
            Buffer.BlockCopy(purePayload, 0, frameData, 2, purePayload.Length);

            _channel.Send(frameData);
        }

        /// <summary>发送原始字节帧（不经过协议序列化）。</summary>
        internal void SendRaw(byte[] frameData)
        {
            if (!IsConnected) return;
            _channel.Send(frameData);
        }

        /// <summary>
        /// 踢出当前会话连接。
        /// </summary>
        /// <param name="reason">踢出原因。</param>
        public void Kick(string reason = "") => _channel?.Close(reason);

        private void HandleDisconnect(IChannel conn, string reason)
        {
            _channel.OnDataReceived -= HandleData;
            _channel.OnDisconnected -= HandleDisconnect;
            OnClosed?.Invoke(this, reason);
        }
    }
}
