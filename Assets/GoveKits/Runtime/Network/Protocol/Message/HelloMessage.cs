


using GoveKits.Network;

namespace GoveKits.Network
{
    // === 握手消息 ===
    [Message(Protocol.HelloID)]
    public class HelloMessage : Message
    {
        public int PlayerID;
        
        public HelloMessage() { }
        public HelloMessage(int id) { PlayerID = id; }
        
        protected override int BodyLength() => 4;
        protected override void BodyWriting(byte[] buffer, ref int index) => WriteInt(buffer, PlayerID, ref index);
        protected override void BodyReading(byte[] buffer, ref int index) => PlayerID = ReadInt(buffer, ref index);
    }
}



