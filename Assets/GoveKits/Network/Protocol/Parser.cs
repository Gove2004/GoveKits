using System;
using UnityEngine;

namespace GoveKits.Network
{
    public class PacketParser
    {
        private byte[] _buffer;
        private int _writeIndex = 0;
        private int _capacity;
        
        // 协议头长度：ID(4) + Length(4)
        private const int HeaderSize = 8;

        // ★ 事件：当一个完整的 Message 被解析出来时触发
        public event Action<Message> OnMessageReady;

        public PacketParser(int capacity = 64 * 1024)
        {
            _capacity = capacity;
            _buffer = new byte[capacity];
        }

        /// <summary>
        /// 接收来自 Socket 的原始数据流
        /// </summary>
        public void InputRawData(byte[] data, int offset, int count)
        {
            // 1. 写入缓冲区
            WriteBuffer(data, offset, count);

            // 2. 循环尝试拆包
            while (TryUnpack(out byte[] payload))
            {
                // 3. 反序列化为 Message 对象
                DeserializeAndNotify(payload);
            }
        }

        private void WriteBuffer(byte[] data, int offset, int count)
        {
            if (_writeIndex + count > _capacity)
            {
                Resize(Math.Max(_capacity * 2, _writeIndex + count));
            }
            Array.Copy(data, offset, _buffer, _writeIndex, count);
            _writeIndex += count;
        }

        // 尝试从缓冲区切出一个完整的包（含Header）
        private bool TryUnpack(out byte[] packet)
        {
            packet = null;
            if (_writeIndex < HeaderSize) return false;

            // 读取 Body 长度 (小端序示例: index 4-7)
            int bodyLen = _buffer[4] | (_buffer[5] << 8) | (_buffer[6] << 16) | (_buffer[7] << 24);
            int totalLen = HeaderSize + bodyLen;

            if (_writeIndex < totalLen) return false;

            // 提取包数据
            packet = new byte[totalLen];
            Array.Copy(_buffer, 0, packet, 0, totalLen);

            // 移动剩余数据到头部
            int remaining = _writeIndex - totalLen;
            if (remaining > 0)
                Array.Copy(_buffer, totalLen, _buffer, 0, remaining);
            
            _writeIndex = remaining;
            return true;
        }

        // 将二进制包转换为 Message 对象并抛出事件
        private void DeserializeAndNotify(byte[] packet)
        {
            if (packet.Length < 4) return;

            // 解析 ID
            int msgId = packet[0] | (packet[1] << 8) | (packet[2] << 16) | (packet[3] << 24);

            try
            {
                // 工厂创建
                Message msg = MessageBuilder.Create<Message>(msgId);
                if (msg != null)
                {
                    int index = Message.HeaderSize;  // 跳过头部
                    msg.Reading(packet, ref index); // 反序列化
                    
                    // ★ 触发事件，流向下一级
                    OnMessageReady?.Invoke(msg);
                }
                else
                {
                    Debug.LogWarning($"[PacketParser] Unknown MsgID: {msgId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PacketParser] Deserialize Error MsgID={msgId}: {ex}");
            }
        }

        private void Resize(int newSize)
        {
            byte[] newBuffer = new byte[newSize];
            Array.Copy(_buffer, 0, newBuffer, 0, _writeIndex);
            _buffer = newBuffer;
            _capacity = newSize;
        }
    }
}