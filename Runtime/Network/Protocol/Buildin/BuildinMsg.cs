using MessagePack;

namespace GoveKits.Runtime.Network
{
    [ProtocolId(1)]
    [MessagePackObject]
    public class HelloServerMsg : IProtocolMessage { }

    [ProtocolId(2)]
    [MessagePackObject]
    public class HelloClientMsg : IProtocolMessage
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string Msg { get; set; }
    }

    [ProtocolId(3)]
    [MessagePackObject]
    public class PingMsg : IProtocolMessage
    {
        [Key(0)] public long ClientTimestamp { get; set; }
        [Key(1)] public long ServerTimestamp { get; set; }
    }

    [ProtocolId(4)]
    [MessagePackObject]
    public class PongMsg : IProtocolMessage
    {
        [Key(0)] public long ClientTimestamp { get; set; }
        [Key(1)] public long ServerTimestamp { get; set; }
    }

    [ProtocolId(5)]
    [MessagePackObject]
    public class ByebyeServerMsg : IProtocolMessage { }

    [ProtocolId(6)]
    [MessagePackObject]
    public class ByebyeClientMsg : IProtocolMessage { }
}