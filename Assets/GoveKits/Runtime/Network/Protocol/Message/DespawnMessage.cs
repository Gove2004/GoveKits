using GoveKits.Binary;

namespace GoveKits.Network
{
    [Message(Protocol.DespawnID, "GoveKits/Runtime/Network/Protocol/Message/Generated")]
    public partial class DespawnMessage : Message
    {
        [BinaryMember(10)] 
        public int NetID;
    }
}