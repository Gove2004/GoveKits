using MessagePack;

namespace GoveKits.Runtime.Network
{
    // 全局唯一的 RPC 请求包
    [ProtocolId(11)]
    [MessagePackObject]
    public class RpcRequestMsg : IProtocolMessage
    {
        [Key(0)] public int RpcId { get; set; }
        [Key(1)] public string MethodName { get; set; } // 路由键
        [Key(2)] public byte[] ArgsData { get; set; }   // 参数盲盒
    }

    // 全局唯一的 RPC 响应包
    [ProtocolId(12)]
    [MessagePackObject]
    public class RpcResponseMsg : IProtocolMessage
    {
        [Key(0)] public int RpcId { get; set; }
        [Key(1)] public int ErrorCode { get; set; } 
        [Key(2)] public string ErrorMsg { get; set; }
        [Key(3)] public byte[] ReturnData { get; set; } // 返回值盲盒
    }
}