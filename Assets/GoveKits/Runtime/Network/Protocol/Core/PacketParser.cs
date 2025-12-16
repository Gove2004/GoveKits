using System;
using Google.Protobuf; // 必须引入


namespace GoveKits.Network
{
    public class PacketParser
    {
        private const int HEADER_SIZE = 4; // Length(4)
        private const int MSG_ID_SIZE = 4; // MsgID(4)
        
        private byte[] _buffer;
        private int _capacity;
        private int _writeIndex;
        private int _readIndex;

        // 回调传递的是 IMessage (Protobuf 基类)
        private readonly Action<IMessage> _onMessageDecoded;

        public PacketParser(Action<IMessage> onMessageDecoded, int initialCapacity = 64 * 1024)
        {
            _onMessageDecoded = onMessageDecoded;
            _capacity = initialCapacity;
            _buffer = new byte[_capacity];
        }

        // 打包: [Length(4) + MsgID(4) + ProtobufData(N)]
        public static byte[] Pack(IMessage msg, out int packetLength)
        {
            int msgId = MessageRegistry.GetId(msg.GetType());
            if (msgId == -1) 
            {
                LogManager.LogError("Parser", $"Msg {msg.GetType().Name} not registered!");
                packetLength = 0;
                return null;
            }

            int bodySize = msg.CalculateSize();
            packetLength = HEADER_SIZE + MSG_ID_SIZE + bodySize;
            
            // 使用 BufferPool 申请内存
            byte[] packet = BufferPool.Rent(packetLength);

            // 1. 写入长度 (BodyLength = MsgID + ProtoData)
            int contentLen = MSG_ID_SIZE + bodySize;
            packet[0] = (byte)(contentLen & 0xFF);
            packet[1] = (byte)((contentLen >> 8) & 0xFF);
            packet[2] = (byte)((contentLen >> 16) & 0xFF);
            packet[3] = (byte)((contentLen >> 24) & 0xFF);

            // 2. 写入 MsgID
            packet[4] = (byte)(msgId & 0xFF);
            packet[5] = (byte)((msgId >> 8) & 0xFF);
            packet[6] = (byte)((msgId >> 16) & 0xFF);
            packet[7] = (byte)((msgId >> 24) & 0xFF);

            // 3. 写入 Protobuf 数据 (Zero Copy)
            var span = new Span<byte>(packet, 8, bodySize);
            msg.WriteTo(span);

            return packet;
        }

        public void Input(ArraySegment<byte> data)
        {
            EnsureCapacity(data.Count);
            Buffer.BlockCopy(data.Array, data.Offset, _buffer, _writeIndex, data.Count);
            _writeIndex += data.Count;
            Parse();
        }

        private void Parse()
        {
            while (_writeIndex - _readIndex >= HEADER_SIZE)
            {
                // 读长度
                int contentLen = _buffer[_readIndex] | (_buffer[_readIndex + 1] << 8) |
                                 (_buffer[_readIndex + 2] << 16) | (_buffer[_readIndex + 3] << 24);

                if (contentLen < 0 || contentLen > 10 * 1024 * 1024) { Reset(); return; }

                int totalLen = HEADER_SIZE + contentLen;
                if (_writeIndex - _readIndex < totalLen) break;

                // 读 MsgID
                int msgIdOffset = _readIndex + HEADER_SIZE;
                int msgId = _buffer[msgIdOffset] | (_buffer[msgIdOffset + 1] << 8) |
                            (_buffer[msgIdOffset + 2] << 16) | (_buffer[msgIdOffset + 3] << 24);

                // 解析 Body
                var parser = MessageRegistry.GetParser(msgId);
                if (parser != null)
                {
                    int bodyOffset = msgIdOffset + MSG_ID_SIZE;
                    int bodyLen = contentLen - MSG_ID_SIZE;
                    
                    try
                    {
                        // ParseFrom 直接支持 byte[] + offset + len
                        IMessage msg = parser.ParseFrom(_buffer, bodyOffset, bodyLen);
                        _onMessageDecoded(msg);
                    }
                    catch (Exception e) { LogManager.LogError("Parser", $"Error: {e}"); }
                }
                else
                {
                    LogManager.LogWarning("Parser", $"Unknown MsgID: {msgId}");
                }

                _readIndex += totalLen;
            }

            if (_readIndex > 0 && _readIndex >= _capacity / 2)
            {
                int remain = _writeIndex - _readIndex;
                if (remain > 0) Buffer.BlockCopy(_buffer, _readIndex, _buffer, 0, remain);
                _writeIndex = remain;
                _readIndex = 0;
            }
        }

        private void EnsureCapacity(int size)
        {
            if (_writeIndex + size <= _capacity) return;
            int newSize = Math.Max(_capacity * 2, _writeIndex + size);
            byte[] newBuf = new byte[newSize];
            Buffer.BlockCopy(_buffer, 0, newBuf, 0, _writeIndex);
            _buffer = newBuf;
            _capacity = newSize;
        }

        private void Reset() { _writeIndex = 0; _readIndex = 0; }
    }
}