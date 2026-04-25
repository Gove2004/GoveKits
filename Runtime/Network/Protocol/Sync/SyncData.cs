
using System.Linq;
using MessagePack;

namespace GoveKits.Runtime.Network
{
    [MessagePackObject]
    [ProtocolId(11)]
    public class PlayerInputPackage : IProtocolMessage
    {
        [Key(0)] public int PlayerId { get; set; }
        [Key(1)] public ushort ProtocolId { get; set; }
        [Key(2)] public byte[] Payload { get; set; }
    }

    [MessagePackObject]
    [ProtocolId(12)]
    public class AllInputPackage : IProtocolMessage
    {
        [Key(0)] public int FrameId { get; set; }
        [Key(1)] public PlayerInputPackage[] Inputs { get; set; }

        public T GetInput<T>(int playerId) where T : class, IProtocolMessage
        {
            if (Inputs == null) return default;
            
            // 【核心修复】：提取时必须核对 ProtocolId，防止 Spawn 被强行解析成 Despawn！
            ushort targetProtoId = ProtocolCore.GetId<T>();
            var input = Inputs.FirstOrDefault(i => i.PlayerId == playerId && i.ProtocolId == targetProtoId);
            
            if (input == null || input.Payload == null) return default;

            return ProtocolCore.Deserialize(targetProtoId, input.Payload) as T;
        }
    }


    [MessagePackObject]
    [ProtocolId(13)]
    public class SyncFrameRequestMsg : IProtocolMessage
    {
        [Key(0)] public int PlayerId { get; set; }
        // 客户端本地当前算到了第几帧？(新加入就是 0)
        [Key(1)] public int LocalFrameId { get; set; } 
    }


    [MessagePackObject]
    [ProtocolId(14)]
    public class SyncFrameResponseMsg : IProtocolMessage
    {
        [Key(0)] public bool IsEnd { get; set; } // 这批是不是最后的数据了？
        [Key(1)] public AllInputPackage[] HistoryFrames; 
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
    [ProtocolId(16)]
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