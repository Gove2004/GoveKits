



using MessagePack;

namespace GoveKits.Runtime.Network
{
    [ProtocolId(1)]
    [MessagePackObject]
    public class HiMsg : IProtocolMessage
    {
        [Key(0)] public int Int;

        public HiMsg(int id) => Int = id;
    }

    [ProtocolId(2)]
    [MessagePackObject]
    public class ByeMsg : IProtocolMessage
    {
        [Key(0)] public int Int;
        [Key(1)] public string Str;
    }
}