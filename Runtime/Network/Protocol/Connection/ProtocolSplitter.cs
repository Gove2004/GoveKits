using System;
using System.Buffers.Binary;

namespace GoveKits.Runtime.Network
{
    public interface IProtocolSplitter
    {
        // 喂入底层连接传来的原始网络切片
        void Feed(ArraySegment<byte> data);
        
        // 尝试提取出一个完整的业务包 (包含: [Id:2] + [Payload:N])
        bool TryExtract(out byte[] packet);
        
        // 断线时清空缓存
        void Clear();
    }

    /// <summary>
    /// 标准的长度前缀拆包器 (前 4 字节为包体长度)
    /// </summary>
    internal class BasicProtocolSplitter : IProtocolSplitter
    {
        private byte[] _buffer;
        private int _writePos;
        private int _readPos;
        private const int HeaderSize = 4;

        public BasicProtocolSplitter(int initialCapacity = 8192)
        {
            _buffer = new byte[initialCapacity];
        }

        public void Feed(ArraySegment<byte> data)
        {
            if (data.Count == 0) return;

            // 如果容量不足，扩容或整理内存
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

            // 读取前4个字节作为长度
            int packetLength = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_buffer, _readPos, HeaderSize));
            
            // 数据包尚未接收完整 (半包)
            if (readableBytes < HeaderSize + packetLength) return false;

            // 提取完整包 (跳过长度头)
            packet = new byte[packetLength];
            Buffer.BlockCopy(_buffer, _readPos + HeaderSize, packet, 0, packetLength);

            _readPos += HeaderSize + packetLength;

            // 如果读完了，复位指针避免碎片化
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