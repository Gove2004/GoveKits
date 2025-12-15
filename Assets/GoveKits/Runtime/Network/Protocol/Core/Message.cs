
using System.Linq.Expressions;
using System.Reflection;
using GoveKits.Binary;


namespace GoveKits.Network
{
    // ===================================================================================
    // 2. 消息基类 (半自动)
    // ===================================================================================
    [Message(-1, "GoveKits/Runtime/Network/Protocol/Core")]
    public partial class Message
    {
        [BinaryMember(1)] public int MsgID;
        
        public Message()
        {
            // 自动获取 ID
            MsgID = MessageBuilder.GetMsgID(this.GetType());
        }
    }

    
}