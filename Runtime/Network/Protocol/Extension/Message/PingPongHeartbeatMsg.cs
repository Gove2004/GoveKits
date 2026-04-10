using MessagePack;


namespace GoveKits.Runtime.Network
{
    [MessagePackObject]
    [ProtocolId(100)]
    public class PingPongHeartbeatMsg : IProtocolMessage
    {
        [Key(0)] public float ClientSendTime;
        [Key(1)] public float ServerRecvTime;  // 服务端回填
    }
}
