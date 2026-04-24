using MessagePack;

namespace GoveKits.Runtime.Network
{
    // 1. 统一的生成指令（一个类包打天下！）
    [MessagePackObject]
    [ProtocolId(303)]
    public class SpawnEntityMsg : IProtocolMessage
    {
        [Key(0)] public int NetId { get; set; }
        [Key(1)] public string PrefabId { get; set; } // 决定生成什么，比如 "Player", "Goblin", "BulletA"
        [Key(2)] public int OwnerId { get; set; } // 归属权：0代表服务器怪物，其他代表玩家
        [Key(3)] public byte[] CustomInitData { get; set; } // 盲盒：装皮肤ID、初始血量、子弹速度等
        
        // 方便业务层拆盲盒的小工具
        public T GetCustomData<T>() where T : class, IProtocolMessage
        {
            if (CustomInitData == null) return default;
            ushort id = ProtocolCore.GetId<T>();
            if (id == 0) return default;
            return ProtocolCore.Deserialize(id, CustomInitData) as T;
        }
    }

    // 2. 销毁指令 (不变)
    [MessagePackObject]
    [ProtocolId(304)]
    public class DespawnEntityMsg : IProtocolMessage
    {
        [Key(0)] public int NetId { get; set; }
    }

    // 3. 断线重连的全家福 (现在变得极其干净！)
    [MessagePackObject]
    [ProtocolId(305)]
    public class SyncAllEntitiesMsg : IProtocolMessage
    {
        [Key(0)] public SpawnEntityMsg[] AllEntities { get; set; }
    }
}