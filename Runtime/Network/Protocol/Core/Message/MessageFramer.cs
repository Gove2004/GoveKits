// Transport/FrameCodec.cs
using System;
using System.Buffers.Binary;
using System.IO;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 网络帧编解码器
    /// 帧结构: [Length:4] + [ProtocolId:2] + [Payload:N]
    /// </summary>
    public static class FrameCodec
    {
        public const int HeaderSize = 6;  // 4 + 2
        public const int MaxPayloadSize = 1024 * 1024; // 1MB 保护

        /// <summary>
        /// 编码消息为帧
        /// </summary>
        public static byte[] Encode(ushort protocolId, ReadOnlySpan<byte> payload)
        {
            int payloadLen = payload.Length;
            int totalLen = HeaderSize + payloadLen;
            
            byte[] frame = new byte[totalLen];
            
            // 长度字段（不包含自身4字节）
            BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), HeaderSize - 4 + payloadLen);
            
            // 协议ID
            BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4, 2), protocolId);
            
            // 负载
            if (payloadLen > 0)
                payload.CopyTo(frame.AsSpan(HeaderSize));
            
            return frame;
        }

        /// <summary>
        /// 尝试解析帧，返回已消费的字节数
        /// </summary>
        public static int TryDecode(ReadOnlySpan<byte> buffer, out ushort protocolId, out ReadOnlySpan<byte> payload)
        {
            protocolId = 0;
            payload = default;

            if (buffer.Length < 4) return 0; // 长度头不足

            int contentLen = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(0, 4));
            
            if (contentLen < 2 || contentLen > MaxPayloadSize)
                throw new InvalidDataException($"非法帧长度: {contentLen}");

            int totalLen = 4 + contentLen;
            if (buffer.Length < totalLen) return 0; // 半包

            protocolId = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(4, 2));
            payload = buffer.Slice(HeaderSize, contentLen - 2);

            return totalLen;
        }
    }
}