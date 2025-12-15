using GoveKits.Binary;

namespace GoveKits.Network
{
    [Message(Protocol.HelloID, "GoveKits/Runtime/Network/Protocol/Message/Generated")]
    public partial class HelloMessage : Message
    {
        [BinaryMember(10)] 
        public int PlayerID = 0;
        [BinaryMember(11)]
        public string Token = "";
        
        public HelloMessage() { }
        public HelloMessage(int id) => PlayerID = id;
        public HelloMessage(string token) => Token = token;
        public HelloMessage(int id, string token)
        {
            PlayerID = id;
            Token = token;
        }
    }
}