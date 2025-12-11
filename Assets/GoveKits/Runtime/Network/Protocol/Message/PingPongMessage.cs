using GoveKits.Binary;

namespace GoveKits.Network
{
    [Message(Protocol.PingPongMsgID, "GoveKits/Runtime/Network/Protocol/Message/Generated")]
    public partial class PingPongMessage : Message
    {
        [BinaryMember(10)] 
        public float Timestamp;

        public PingPongMessage() { }
        public PingPongMessage(float timestamp) => Timestamp = timestamp;
    }
}