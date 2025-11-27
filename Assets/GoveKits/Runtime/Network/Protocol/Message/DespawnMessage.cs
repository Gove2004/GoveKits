


namespace GoveKits.Network
{
    // === 销毁物体消息 ===
    [Message(Protocol.DespawnID)]
    public class DespawnMessage : Message
    {
        public int NetID;
        protected override int BodyLength() => 4;
        protected override void BodyWriting(byte[] b, ref int i) => WriteInt(b, NetID, ref i);
        protected override void BodyReading(byte[] b, ref int i) => NetID = ReadInt(b, ref i);
    }
}