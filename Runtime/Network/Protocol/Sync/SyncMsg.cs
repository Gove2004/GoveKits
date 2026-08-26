using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace GoveKits.Runtime.Network
{
    /// <summary>玩家输入数据包，由客户端每帧提交。</summary>
    [MessagePackObject]
    [ProtocolId(7)]
    public class PlayerInputPackage : IProtocolMessage
    {
        /// <summary>玩家 ID。</summary>
        [Key(0)] public int PlayerId;
        /// <summary>序列化后的输入载荷。</summary>
        [Key(1)] public byte[] Payload;
    }

    /// <summary>服务端聚合的所有玩家输入集合。</summary>
    public class AllInputPackage : IProtocolMessage
    {
        /// <summary>服务器同步 Tick 编号。</summary>
        public int Tick;
        /// <summary>所有实体的状态载荷字典，键为实体 ID。</summary>
        public Dictionary<int, byte[]> Inputs = new();

        /// <summary>
        /// 从 ProtocolCenter 反序列化指定玩家的输入数据。
        /// </summary>
        public T GetInput<T>(ProtocolCenter protocol, int playerId) where T : class
        {
            if (Inputs.TryGetValue(playerId, out var payload))
            {
                return protocol.Deserialize(typeof(T), payload) as T;
            }
            return default;
        }
    }

    /// <summary>单个实体的状态快照。</summary>
    [MessagePackObject]
    public class EntityState
    {
        /// <summary>实体网络 ID。</summary>
        [Key(0)] public uint NetId;
        /// <summary>序列化后的状态载荷。</summary>
        [Key(1)] public byte[] StatePayload;

        /// <summary>
        /// 从 ProtocolCenter 反序列化状态数据。
        /// </summary>
        public T GetState<T>(ProtocolCenter protocol) where T : class
        {
            if (StatePayload == null) return default;
            ushort id = protocol.GetId<T>();
            if (id == 0) return default;
            return protocol.Deserialize(id, StatePayload) as T;
        }
    }

    /// <summary>
    /// 世界状态包，包含所有实体的状态快照和 Tick 编号。
    /// </summary>
    [MessagePackObject]
    [ProtocolId(8)]
    public class WorldPackage : IProtocolMessage
    {
        /// <summary>同步 Tick 编号。</summary>
        [Key(0)] public int Tick;
        /// <summary>实体状态数组。</summary>
        [Key(1)] public EntityState[] Entities;

        /// <summary>根据实体 ID 查找对应的状态快照。</summary>
        /// <param name="netId">实体网络 ID。</param>
        /// <returns>匹配的状态快照，未找到则返回 null。</returns>
        public EntityState GetEntity(int netId)
        {
            if (Entities == null) return null;
            return Array.Find(Entities, e => e.NetId == netId);
        }
    }

    /// <summary>
    /// 实体生成消息，用于在服务端通知客户端生成新实体。
    /// </summary>
    [MessagePackObject]
    [ProtocolId(9)]
    public class SpawnMsg : IProtocolMessage
    {
        /// <summary>生成对象的唯一 ID。</summary>
        [Key(0)] public uint ObjectId { get; set; }

        /// <summary>生成键，用于查找预设或配置。</summary>
        [Key(1)] public string SpawnKey { get; set; }

        /// <summary>生成数据（序列化后的字节载荷）。</summary>
        [Key(2)] public byte[] SpawnData { get; set; }

        /// <summary>生成数据的类型。</summary>
        [Key(3)] public Type SpawnDataType { get; set; }

        public SpawnMsg() { }
        public SpawnMsg(uint objectId, string spawnKey, byte[] spawnData, Type spawnDataType)
        {
            ObjectId = objectId;
            SpawnKey = spawnKey;
            SpawnData = spawnData;
            SpawnDataType = spawnDataType;
        }
    }

    /// <summary>
    /// 实体消亡消息，用于在服务端通知客户端销毁实体。
    /// </summary>
    [MessagePackObject]
    [ProtocolId(10)]
    public class DespawnMsg : IProtocolMessage
    {
        /// <summary>要销毁的对象唯一 ID。</summary>
        [Key(0)] public uint ObjectId { get; set; }
        public DespawnMsg() { }
        public DespawnMsg(uint objectId)
        {
            ObjectId = objectId;
        }
    }
}
