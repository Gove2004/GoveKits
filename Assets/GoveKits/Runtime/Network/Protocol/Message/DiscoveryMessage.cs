

using System.Text;

namespace GoveKits.Network
{
    // === 发现消息 ===
    [Message(Protocol.DiscoveryID)]
    public class DiscoveryMessage : Message
    {
        public string Info;
        public DiscoveryMessage() { }
        public DiscoveryMessage(string info) => Info = info;
        protected override int BodyLength() => 4 + Encoding.UTF8.GetByteCount(Info ?? "");
        protected override void BodyWriting(byte[] buffer, ref int index) => WriteString(buffer, Info ?? "", ref index);
        protected override void BodyReading(byte[] buffer, ref int index) => Info = ReadString(buffer, ref index);
    }
}