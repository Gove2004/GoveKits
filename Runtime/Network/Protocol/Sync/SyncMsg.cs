
using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace GoveKits.Runtime.Network
{
    [MessagePackObject]
    [ProtocolId(7)]
    public class PlayerInputPackage : IProtocolMessage
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public byte[] Payload;
    }


    public class AllInputPackage : IProtocolMessage
    {
        public int Tick;
        public Dictionary<int, byte[]> Inputs = new();

        public T GetInput<T>(int playerId) where T : class
        {
            if (Inputs.TryGetValue(playerId, out var payload))
            {
                return ProtocolCore.Deserialize(typeof(T), payload) as T;
            }
            return default;
        }
    }

    // 2. 服务端记录的单个实体状态
    [MessagePackObject]
    public class EntityState
    {
        [Key(0)] public uint NetId;
        [Key(1)] public byte[] StatePayload;

        public T GetState<T>() where T : class
        {
            if (StatePayload == null) return default;
            ushort id = ProtocolCore.GetId<T>();
            if (id == 0) return default;
            return ProtocolCore.Deserialize(id, StatePayload) as T;
        }
    }

    // 3. 服务端定时下发的世界快照包
    [MessagePackObject]
    [ProtocolId(8)]
    public class WorldPackage : IProtocolMessage
    {
        [Key(0)] public int Tick;
        [Key(1)] public EntityState[] Entities;

        public EntityState GetEntity(int netId)
        {
            if (Entities == null) return null;
            return Entities.FirstOrDefault(e => e.NetId == netId);
        }
    }




    
    [MessagePackObject]
    [ProtocolId(9)]
    public class SpawnMsg : IProtocolMessage
    {
        [Key(0)] public uint ObjectId { get; set; }
        [Key(1)] public string SpawnKey { get; set; }
        [Key(2)] public byte[] SpawnData { get; set; } // 盲盒数据
        [Key(3)] public Type SpawnDataType { get; set; } // 盲盒数据的类型

        public SpawnMsg() { }
        public SpawnMsg(uint objectId, string spawnKey, byte[] spawnData, Type spawnDataType)
        {
            ObjectId = objectId;
            SpawnKey = spawnKey;
            SpawnData = spawnData;
            SpawnDataType = spawnDataType;
        }
    }

    [MessagePackObject]
    [ProtocolId(10)]
    public class DespawnMsg : IProtocolMessage
    {
        [Key(0)] public uint ObjectId { get; set; }
        public DespawnMsg() { }
        public DespawnMsg(uint objectId)
        {
            ObjectId = objectId;
        }
    }
}