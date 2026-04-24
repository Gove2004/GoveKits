using MessagePack;
using System.Linq;

namespace GoveKits.Runtime.Network
{
    // 1. 客户端提交给服务器的意图包
    [MessagePackObject]
    [ProtocolId(301)]
    public class StateInputPackage : IProtocolMessage
    {
        [Key(0)] public int PlayerId { get; set; }
        [Key(1)] public byte[] Payload { get; set; } // 业务层数据盲盒
    }

    // 2. 服务端记录的单个实体状态
    [MessagePackObject]
    public class EntityState
    {
        [Key(0)] public int NetId { get; set; }
        // 坐标与旋转 (拆分 float 确保 MessagePack 完美序列化)
        [Key(1)] public float Px { get; set; }
        [Key(2)] public float Py { get; set; }
        [Key(3)] public float Pz { get; set; }
        [Key(4)] public float Rx { get; set; }
        [Key(5)] public float Ry { get; set; }
        [Key(6)] public float Rz { get; set; }
        
        [Key(7)] public byte[] Payload { get; set; } // 业务自定义状态盲盒（血量、动画等）

        public T GetCustomState<T>() where T : class, IProtocolMessage
        {
            if (Payload == null) return default;
            ushort id = ProtocolCore.GetId<T>();
            if (id == 0) return default;
            return ProtocolCore.Deserialize(id, Payload) as T;
        }
    }

    // 3. 服务端定时下发的世界快照包
    [MessagePackObject]
    [ProtocolId(302)]
    public class WorldPackage : IProtocolMessage
    {
        [Key(0)] public int SnapshotTick { get; set; }
        [Key(1)] public float ServerTime { get; set; }
        [Key(2)] public EntityState[] Entities { get; set; }

        public EntityState GetEntity(int netId)
        {
            if (Entities == null) return null;
            return Entities.FirstOrDefault(e => e.NetId == netId);
        }
    }
}