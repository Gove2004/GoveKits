


namespace GoveKits.Network
{
    // === PingPong 消息 ===
    [Message(Protocol.PingPongMsgID)]
    public class PingPongMessage : Message
    {
        public float Timestamp; // 发送时间戳

        public PingPongMessage() { }

        public PingPongMessage(float timestamp)
        {
            Timestamp = timestamp;
        }
        protected override int BodyLength() => 4;
        protected override void BodyWriting(byte[] buffer, ref int index) => WriteFloat(buffer, Timestamp, ref index);
        protected override void BodyReading(byte[] buffer, ref int index) => Timestamp = ReadFloat(buffer, ref index);
    }
}