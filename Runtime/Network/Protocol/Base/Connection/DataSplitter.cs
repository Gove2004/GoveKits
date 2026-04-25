using System;
using System.Buffers.Binary;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 标准的长度前缀拆包器，被 Connection 持有，处理粘包与半包
    /// </summary>
    public class DataSplitter
    {
        private byte[] _buffer;
        private int _writePos;
        private int _readPos;
        private const int HeaderSize = 4; // 4字节长度头

        public DataSplitter(int initialCapacity = 8192)
        {
            _buffer = new byte[initialCapacity];
        }

        /// <summary>
        /// 喂入底层 Socket 收到的原始切片数据
        /// </summary>
        public void Feed(ArraySegment<byte> data)
        {
            if (data.Count == 0) return;

            // 扩容或整理内存碎片
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

        /// <summary>
        /// 尝试提取出一个完整的网络帧 (去除 4 字节长度头)
        /// </summary>
        public bool TryExtract(out byte[] packet)
        {
            packet = null;
            int readableBytes = _writePos - _readPos;

            if (readableBytes < HeaderSize) return false;

            int packetLength = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_buffer, _readPos, HeaderSize));
            
            // 保护机制：非法包长
            if (packetLength <= 0 || packetLength > 1024 * 1024 * 5)
                throw new Exception($"Invalid packet length: {packetLength}");

            // 半包：数据包尚未接收完整
            if (readableBytes < HeaderSize + packetLength) return false;

            // 提取完整包体
            packet = new byte[packetLength];
            Buffer.BlockCopy(_buffer, _readPos + HeaderSize, packet, 0, packetLength);

            _readPos += HeaderSize + packetLength;

            // 复位指针
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