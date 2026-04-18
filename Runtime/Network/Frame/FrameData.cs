using MessagePack;
using System.Linq;

namespace GoveKits.Runtime.Network
{
    [MessagePackObject]
    [ProtocolId(101)]
    public class FrameInputPackage : IProtocolMessage
    {
        [Key(0)] public int PlayerId { get; set; }
        [Key(1)] public byte[] Payload { get; set; } // 业务层数据盲盒
    }


    [MessagePackObject]
    [ProtocolId(102)]
    public class FramePackage : IProtocolMessage
    {
        [Key(0)] public int FrameId { get; set; }
        [Key(1)] public FrameInputPackage[] Inputs { get; set; }

        public T GetInput<T>(int playerId) where T : IProtocolMessage
        {
            if (Inputs == null) return default;
            var input = Inputs.FirstOrDefault(i => i.PlayerId == playerId);
            if (input == null || input.Payload == null) return default;
            return ProtocolCore.Deserialize<T>(input.Payload);
        }

        public bool HasInput(int playerId)
        {
            return Inputs != null && Inputs.Any(i => i.PlayerId == playerId);
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