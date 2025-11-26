

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Network
{
    // === 解析器 ===
    public class PacketParser
    {
        private const int MaxPacketSize = 1024 * 1024 * 2;
        private byte[] _buffer = new byte[64 * 1024];
        private int _writeIndex = 0;
        private int _readIndex = 0;
        private const int LengthSize = 4;
        private readonly Func<Message, UniTask> _onMessageDecoded;

        public PacketParser(Func<Message, UniTask> onMessageDecoded) => _onMessageDecoded = onMessageDecoded;

        public static byte[] PackMessage(Message msg)
        {
            int totalLen = msg.Length();
            byte[] packet = new byte[4 + totalLen];
            int index = 4;
            msg.Writing(packet, ref index);
            int bodyLen = index - 4;
            packet[0] = (byte)(bodyLen & 0xFF);
            packet[1] = (byte)((bodyLen >> 8) & 0xFF);
            packet[2] = (byte)((bodyLen >> 16) & 0xFF);
            packet[3] = (byte)((bodyLen >> 24) & 0xFF);
            return packet;
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
            while (_writeIndex - _readIndex >= LengthSize)
            {
                int bodyLen = _buffer[_readIndex] | (_buffer[_readIndex + 1] << 8) |
                              (_buffer[_readIndex + 2] << 16) | (_buffer[_readIndex + 3] << 24);
                int fullLen = LengthSize + bodyLen;

                if (bodyLen < 0 || bodyLen > MaxPacketSize) { _readIndex = _writeIndex; return; }
                if (_writeIndex - _readIndex < fullLen) break;

                int msgIdOffset = _readIndex + LengthSize;
                int msgId = _buffer[msgIdOffset] | (_buffer[msgIdOffset + 1] << 8) |
                            (_buffer[msgIdOffset + 2] << 16) | (_buffer[msgIdOffset + 3] << 24);

                try
                {
                    Message msg = MessageBuilder.Create<Message>(msgId);
                    if (msg != null)
                    {
                        int payloadIndex = _readIndex + LengthSize;
                        msg.Reading(_buffer, ref payloadIndex);
                        _onMessageDecoded?.Invoke(msg).Forget();
                    }
                }
                catch (Exception ex) { Debug.LogError($"[Parser] Decode Error: {ex}"); }
                _readIndex += fullLen;
            }
            if (_readIndex > 0 && _readIndex >= _buffer.Length / 2)
            {
                int remain = _writeIndex - _readIndex;
                if (remain > 0) Array.Copy(_buffer, _readIndex, _buffer, 0, remain);
                _writeIndex = remain; _readIndex = 0;
            }
        }
        
        private void EnsureCapacity(int count)
        {
            if (_writeIndex + count <= _buffer.Length) return;
            if (_readIndex > 0) {
                int remain = _writeIndex - _readIndex;
                Array.Copy(_buffer, _readIndex, _buffer, 0, remain);
                _writeIndex = remain; _readIndex = 0;
            }
            if (_writeIndex + count > _buffer.Length) {
                int newSize = Math.Max(_buffer.Length * 2, _writeIndex + count);
                byte[] newBuf = new byte[newSize];
                Array.Copy(_buffer, 0, newBuf, 0, _writeIndex);
                _buffer = newBuf;
            }
        }
    }
}