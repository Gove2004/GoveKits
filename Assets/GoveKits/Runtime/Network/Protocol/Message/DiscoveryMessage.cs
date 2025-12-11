using GoveKits.Binary;

namespace GoveKits.Network
{
    [Message(Protocol.DiscoveryID, "GoveKits/Runtime/Network/Protocol/Message/Generated")]
    public partial class DiscoveryMessage : Message
    {
        [BinaryMember(10)] 
        public string Info;

        public DiscoveryMessage() { }
        public DiscoveryMessage(string info) => Info = info;
    }
}