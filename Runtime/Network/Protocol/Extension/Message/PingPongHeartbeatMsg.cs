using MessagePack;


namespace GoveKits.Runtime.Network
{
    [MessagePackObject]
    [ProtocolId(100)]
    public class PingPongHeartbeatMsg : IProtocolMessage
    {
        [Key(0)] public long ClientSendTime;
        [Key(1)] public long ServerRecvTime;  // 服务端回填
    }
}
