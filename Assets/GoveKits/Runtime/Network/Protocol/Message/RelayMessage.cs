using System.Collections.Generic;
using GoveKits.Binary;
using UnityEngine;

namespace GoveKits.Network
{
    [Message(Protocol.RelayID, "GoveKits/Runtime/Network/Protocol/Message/Generated")]
    public partial class RelayMessage : Message
    {
        [BinaryMember(10)] public int targetId;
        [BinaryMember(11)] public int InnerMsgID;
        
        [BinaryMember(12)] public List<byte> InnerData; 
        
        [BinaryMember(13)] public List<int> ExcludeIDs; 

        public RelayMessage() { }

        public RelayMessage(int targetId, Message innerMsg, List<int> excludeIDs = null)
        {
            this.targetId = targetId;
            InnerMsgID = innerMsg.MsgID;
            ExcludeIDs = excludeIDs;
            
            // 序列化内部消息
            int len = innerMsg.Length();
            byte[] InnerBuff = new byte[len];
            int index = 0;
            innerMsg.Writing(InnerBuff, ref index);
            InnerData = new List<byte>(InnerBuff);
        }

        public T GetMessage<T>() where T : Message
        {
            Message innerMsg = MessageBuilder.Create<Message>(InnerMsgID);
            if (innerMsg == null) return null;

            int index = 0;
            innerMsg.Reading(InnerData.ToArray(), ref index, InnerData.Count);
            return innerMsg as T;
        }
    }
}