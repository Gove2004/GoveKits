using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public class PacketParser
    {
        private const int HEADER_SIZE = 4;
        private byte[] _buffer;
        private int _capacity;
        private int _writeIndex;
        private int _readIndex;

        private readonly Action<Message> _onMessageDecoded;

        public PacketParser(Action<Message> onMessageDecoded, int initialCapacity = 64 * 1024)
        {
            _onMessageDecoded = onMessageDecoded;
            _capacity = initialCapacity;
            _buffer = new byte[_capacity];
        }

        // 打包：Length(4) + Body(MsgID + Content)
        public static byte[] Pack(Message msg, out int length)
        {
            int bodyLen = msg.Length();
            length = HEADER_SIZE + bodyLen;
            
            // 发送频率低时 new byte[] 可接受，若极高频可用 BufferPool 优化
            byte[] packet = new byte[length];

            // 写入长度 (Little Endian)
            packet[0] = (byte)(bodyLen & 0xFF);
            packet[1] = (byte)((bodyLen >> 8) & 0xFF);
            packet[2] = (byte)((bodyLen >> 16) & 0xFF);
            packet[3] = (byte)((bodyLen >> 24) & 0xFF);

            int index = 4;
            msg.Writing(packet, ref index);
            return packet;
        }

        // 接收切片数据
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
                int bodyLen = _buffer[_readIndex] | (_buffer[_readIndex + 1] << 8) |
                              (_buffer[_readIndex + 2] << 16) | (_buffer[_readIndex + 3] << 24);

                if (bodyLen < 0 || bodyLen > 10 * 1024 * 1024) { Reset(); return; }

                int totalLen = HEADER_SIZE + bodyLen;
                if (_writeIndex - _readIndex < totalLen) break;

                // 解析 MsgID
                int msgIdOffset = _readIndex + HEADER_SIZE;
                int msgId = _buffer[msgIdOffset] | (_buffer[msgIdOffset + 1] << 8) |
                            (_buffer[msgIdOffset + 2] << 16) | (_buffer[msgIdOffset + 3] << 24);

                try
                {
                    Message msg = MessageBuilder.Create<Message>(msgId);
                    if (msg != null)
                    {
                        int payloadStart = _readIndex + HEADER_SIZE;
                        msg.Reading(_buffer, ref payloadStart, bodyLen);
                        _onMessageDecoded(msg);
                    }
                }
                catch (Exception e) { Debug.LogError($"[Parser] Error: {e}"); }

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