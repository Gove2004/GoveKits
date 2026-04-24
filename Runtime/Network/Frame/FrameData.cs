using MessagePack;
using System.Linq;

namespace GoveKits.Runtime.Network
{
    [MessagePackObject]
    [ProtocolId(101)]
    public class FrameInputPackage : IProtocolMessage
    {
        [Key(0)] public int PlayerId { get; set; }
        [Key(1)] public ushort ProtocolId { get; set; } // 辨别Payload
        [Key(2)] public byte[] Payload { get; set; } 
    }

    [MessagePackObject]
    [ProtocolId(102)]
    public class FramePackage : IProtocolMessage
    {
        [Key(0)] public int FrameId { get; set; }
        [Key(1)] public FrameInputPackage[] Inputs { get; set; }

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
    [ProtocolId(103)]
    public class SyncFrameRequestMsg : IProtocolMessage
    {
        [Key(0)] public int PlayerId { get; set; }
        // 客户端本地当前算到了第几帧？(新加入就是 0)
        [Key(1)] public int LocalFrameId { get; set; } 
    }


    [MessagePackObject]
    [ProtocolId(104)]
    public class SyncFrameResponseMsg : IProtocolMessage
    {
        [Key(0)] public bool IsEnd { get; set; } // 这批是不是最后的数据了？
        [Key(1)] public FramePackage[] HistoryFrames; 
    }
}