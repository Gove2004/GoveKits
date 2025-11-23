using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    public class PacketParser
    {
        private byte[] _buffer;
        private int _writeIndex = 0;
        private int _readIndex = 0;
        private const int LengthSize = 4; // 长度头占4字节

        private readonly Func<Message, UniTask> _onMessageDecoded;  // 解码后消息回调

        public PacketParser(Func<Message, UniTask> onMessageDecoded, int capacity = 64 * 1024)
        {
            _buffer = new byte[capacity];
            _onMessageDecoded = onMessageDecoded;
        }

        public void InputRawData(byte[] data, int offset, int count)
        {
            EnsureCapacity(count);
            Array.Copy(data, offset, _buffer, _writeIndex, count);
            _writeIndex += count;
            Parse();
        }

        private void Parse()
        {
            // 循环直到数据不足
            while (_writeIndex - _readIndex >= LengthSize)
            {
                // 1. 读包体长度 (假设小端序: Low byte first)
                int bodyLen = _buffer[_readIndex] | (_buffer[_readIndex + 1] << 8) |
                              (_buffer[_readIndex + 2] << 16) | (_buffer[_readIndex + 3] << 24);

                int fullPacketLen = LengthSize + bodyLen;

                // 2. 检查数据是否足够一个整包
                if (_writeIndex - _readIndex < fullPacketLen) break;

                // 3. 读取 MsgID (Header的前4个字节)
                int msgIdOffset = _readIndex + LengthSize;
                int msgId = _buffer[msgIdOffset] | (_buffer[msgIdOffset + 1] << 8) |
                            (_buffer[msgIdOffset + 2] << 16) | (_buffer[msgIdOffset + 3] << 24);

                try
                {
                    Message msg = MessageBuilder.Create<Message>(msgId);
                    if (msg != null)
                    {
                        // 跳过 Length (4字节)
                        int payloadIndex = _readIndex + LengthSize;
                        
                        // 传入 ref index，Message 内部会自动读取 MsgID -> Header -> Body
                        msg.Reading(_buffer, ref payloadIndex);
                        
                        // 推入分发器
                        _onMessageDecoded?.Invoke(msg).Forget();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PacketParser] Parse Error MsgID:{msgId} - {ex}");
                }

                // 4. 移动指针
                _readIndex += fullPacketLen;
            }

            // 5. 内存整理：如果读指针过半，将剩余数据搬运到头部
            if (_readIndex >= _buffer.Length / 2 && _readIndex > 0)
            {
                int remain = _writeIndex - _readIndex;
                if (remain > 0) Array.Copy(_buffer, _readIndex, _buffer, 0, remain);
                _writeIndex = remain;
                _readIndex = 0;
            }
        }

        private void EnsureCapacity(int count)
        {
            if (_writeIndex + count <= _buffer.Length) return;
            
            // 先尝试通过整理内存腾出空间
            if (_readIndex > 0)
            {
                int remain = _writeIndex - _readIndex;
                Array.Copy(_buffer, _readIndex, _buffer, 0, remain);
                _writeIndex = remain;
                _readIndex = 0;
            }

            // 如果还是不够，扩容
            if (_writeIndex + count > _buffer.Length)
            {
                int newSize = Math.Max(_buffer.Length * 2, _writeIndex + count);
                byte[] newBuf = new byte[newSize];
                Array.Copy(_buffer, 0, newBuf, 0, _writeIndex);
                _buffer = newBuf;
            }
        }
    }
}