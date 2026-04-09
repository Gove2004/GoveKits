// === MessageFramer.cs ===
using System;
using UnityEngine;

namespace GoveKits.Runtime.Network.Core
{
    public static class MessageFramer
    {
        // 打包：将 MsgId 和 Protobuf的字节数组，打包成最终要在网线上跑的包
        public static byte[] Pack(int msgId, byte[] payload)
        {
            int payloadLen = payload != null ? payload.Length : 0;
            int totalLen = 4 + 4 + payloadLen; // 长度(4) + MsgId(4) + 载荷(N)
            
            byte[] packet = new byte[totalLen];
            
            // 1. 写入总长度 (除去表示长度的这4个字节外，剩余数据的长度)
            int contentLen = 4 + payloadLen; 
            Buffer.BlockCopy(BitConverter.GetBytes(contentLen), 0, packet, 0, 4);
            
            // 2. 写入 MsgId
            Buffer.BlockCopy(BitConverter.GetBytes(msgId), 0, packet, 4, 4);
            
            // 3. 写入 Payload
            if (payloadLen > 0)
            {
                Buffer.BlockCopy(payload, 0, packet, 8, payloadLen);
            }
            
            return packet;
        }

        // 解包：处理收到的字节流，把完整的包切出来
        // 返回：读取消耗的字节数。如果返回0，说明包还没收全。
        public static int TryParse(byte[] buffer, int bytesAvailable, out int msgId, out byte[] payload)
        {
            msgId = 0;
            payload = null;

            if (bytesAvailable < 4) return 0; // 连长度头都没收齐

            int contentLen = BitConverter.ToInt32(buffer, 0);
            int totalLen = 4 + contentLen;

            // 防御性保护：防止收到脏数据导致内存爆炸
            if (contentLen < 0 || contentLen > 10 * 1024 * 1024) 
                throw new Exception("Invalid packet length!");

            if (bytesAvailable < totalLen) return 0; // 半包，等下次数据

            msgId = BitConverter.ToInt32(buffer, 4);
            
            int payloadLen = contentLen - 4;
            payload = new byte[payloadLen];
            if (payloadLen > 0)
            {
                Buffer.BlockCopy(buffer, 8, payload, 0, payloadLen);
            }

            return totalLen; // 返回这个完整包占据的总字节数
        }
    }
}