using GoveKits.Binary;

namespace GoveKits.Network
{
    [Message(Protocol.HelloID, "GoveKits/Runtime/Network/Protocol/Message/Generated")]
    public partial class HelloMessage : Message
    {
        [BinaryMember(10)] 
        public int PlayerID;
        
        public HelloMessage() { }
        public HelloMessage(int id) => PlayerID = id;
    }
}