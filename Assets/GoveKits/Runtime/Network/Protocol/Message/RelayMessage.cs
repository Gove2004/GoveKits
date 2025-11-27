using System;
using UnityEngine;

namespace GoveKits.Network
{
    /// <summary>
    /// 中转信封：用于客户端请求服务器转发消息
    /// </summary>
    [Message(Protocol.RelayID)]
    public class RelayMessage : Message
    {
        public int targetId;
        public int InnerMsgID;    // 内部消息的协议ID
        public byte[] InnerData;  // 内部消息的序列化数据
        public int[] ExcludeIDs;

        public RelayMessage() { }

        // 构造函数：自动将一个普通消息打包成中转消息
        public RelayMessage(int targetId, Message innerMsg, int[] excludeIDs = null)
        {
            this.targetId = targetId;
            InnerMsgID = innerMsg.MsgID;
            ExcludeIDs = excludeIDs;
            
            // 序列化内部消息
            int len = innerMsg.Length();
            InnerData = new byte[len];
            int index = 0;
            innerMsg.Writing(InnerData, ref index);
        }

        public T GetMessage<T>() where T : Message
        {
            // 根据 InnerMsgID 创建对应的消息实例
            Message innerMsg = MessageBuilder.Create<Message>(InnerMsgID);
            if (innerMsg == null)
            {
                Debug.LogError($"RelayMessage: Unknown InnerMsgID {InnerMsgID}");
                return null;
            }

            // 反序列化内部消息
            int index = 0;
            innerMsg.Reading(InnerData, ref index);
            return innerMsg as T;
        }

        // targetId + InnerMsgID + InnerData.Length + InnerData
        protected override int BodyLength() => 4 + 4 + 4 + (InnerData?.Length ?? 0) + 4 + (ExcludeIDs?.Length ?? 0) * 4;

        protected override void BodyWriting(byte[] b, ref int i)
        {
            WriteInt(b, targetId, ref i);
            WriteInt(b, InnerMsgID, ref i);
            WriteBytes(b, InnerData, ref i);
            WriteInt(b, ExcludeIDs?.Length ?? 0, ref i);
            if (ExcludeIDs != null)
            {
                foreach (var id in ExcludeIDs)
                {
                    WriteInt(b, id, ref i);
                }
            }
        }

        protected override void BodyReading(byte[] b, ref int i)
        {
            targetId = ReadInt(b, ref i);
            InnerMsgID = ReadInt(b, ref i);
            InnerData = ReadBytes(b, ref i);
            if (InnerData == null)
                InnerData = new byte[0];
            int excludeCount = ReadInt(b, ref i);
            if (excludeCount > 0)
            {
                ExcludeIDs = new int[excludeCount];
                for (int idx = 0; idx < excludeCount; idx++)
                {
                    ExcludeIDs[idx] = ReadInt(b, ref i);
                }
            }
            else
            {
                ExcludeIDs = null;
            }
        }
    }
}