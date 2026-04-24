namespace GoveKits.Runtime.Network
{
    // RPC 请求包必须实现此接口
    public interface IRpcRequest : IProtocolMessage
    {
        int RpcId { get; set; }
    }

    // RPC 响应包必须实现此接口
    public interface IRpcResponse : IProtocolMessage
    {
        int RpcId { get; set; }
        int ErrorCode { get; set; } // 0表示成功，非0表示错误
        string ErrorMsg { get; set; }
    }
}