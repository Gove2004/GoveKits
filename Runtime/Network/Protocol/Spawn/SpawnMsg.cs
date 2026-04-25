

using MessagePack;

namespace GoveKits.Runtime.Network
{
    [MessagePackObject]
    [ProtocolId(31)]
    public class SpawnReqMsg : IProtocolMessage
    {
        [Key(0)] public string PrefabId { get; set; }
        [Key(1)] public byte[] CustomInitData { get; set; }
    }

    [MessagePackObject]
    [ProtocolId(32)]
    public class SpawnRspMsg : IProtocolMessage
    {
        [Key(0)] public string PrefabId { get; set; }
        [Key(1)] public uint ObjectId { get; set; }
        [Key(2)] public byte[] CustomInitData { get; set; }
    }

    [MessagePackObject]
    [ProtocolId(33)]
    public class DespawnReqMsg : IProtocolMessage
    {
        [Key(0)] public uint ObjectId { get; set; }

        public DespawnReqMsg() { }
        public DespawnReqMsg(uint objectId) => ObjectId = objectId;
    }

    [MessagePackObject]
    [ProtocolId(34)]
    public class DespawnRspMsg : IProtocolMessage
    {
        [Key(0)] public uint ObjectId { get; set; }

        public DespawnRspMsg() { }
        public DespawnRspMsg(uint objectId) => ObjectId = objectId;
    }
}