using System;
using System.Buffers.Binary;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 长度前缀拆包器，处理 TCP 粘包与半包问题。
    /// 协议格式：[4 字节长度（小端）][N 字节 payload]。
    /// </summary>
    public class DataSplitter
    {
        private byte[] _buffer;
        private int _writePos;
        private int _readPos;
        private const int HeaderSize = 4;
        private const int MaxPacketSize = 1024 * 1024 * 5;

        public DataSplitter(int initialCapacity = 8192)
        {
            _buffer = new byte[initialCapacity];
        }

        public void Feed(ArraySegment<byte> data)
        {
            if (data.Count == 0) return;

            if (_writePos + data.Count > _buffer.Length)
            {
                int validDataCount = _writePos - _readPos;
                if (validDataCount + data.Count > _buffer.Length)
                {
                    Array.Resize(ref _buffer, (validDataCount + data.Count) * 2);
                }

                if (validDataCount > 0 && _readPos > 0)
                {
                    Buffer.BlockCopy(_buffer, _readPos, _buffer, 0, validDataCount);
                }
                _readPos = 0;
                _writePos = validDataCount;
            }

            Buffer.BlockCopy(data.Array, data.Offset, _buffer, _writePos, data.Count);
            _writePos += data.Count;
        }

        public bool TryExtract(out byte[] packet)
        {
            packet = null;
            int readableBytes = _writePos - _readPos;

            if (readableBytes < HeaderSize) return false;

            int packetLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(_buffer, _readPos, HeaderSize));

            if (packetLength == 0 || packetLength > MaxPacketSize)
                return false;

            if (readableBytes < HeaderSize + packetLength) return false;

            packet = new byte[packetLength];
            Buffer.BlockCopy(_buffer, _readPos + HeaderSize, packet, 0, packetLength);

            _readPos += HeaderSize + packetLength;

            if (_readPos == _writePos)
            {
                _readPos = 0;
                _writePos = 0;
            }

            return true;
        }

        public void Clear()
        {
            _readPos = 0;
            _writePos = 0;
        }
    }
}
