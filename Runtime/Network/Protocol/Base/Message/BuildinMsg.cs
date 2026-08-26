using MessagePack;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 协议 ID 1：客户端向服务端发送的初始握手消息。
    /// 建立连接后由客户端首先发出，用于通知服务端存在新连接。
    /// </summary>
    [ProtocolId(1)]
    [MessagePackObject]
    public class HelloServerMsg : IProtocolMessage { }

    /// <summary>
    /// 协议 ID 2：服务端对 HelloServerMsg 的握手响应。
    /// 包含服务端分配给该客户端的唯一 PlayerId 和欢迎信息。
    /// </summary>
    [ProtocolId(2)]
    [MessagePackObject]
    public class HelloClientMsg : IProtocolMessage
    {
        /// <summary>服务端分配的客户端唯一标识。</summary>
        [Key(0)] public int Id { get; set; }
        /// <summary>欢迎文本信息。</summary>
        [Key(1)] public string Msg { get; set; }
    }

    /// <summary>
    /// 协议 ID 3：心跳探测请求消息。
    /// 客户端定期发送以检测连接存活并测量往返延迟（RTT）。
    /// </summary>
    [ProtocolId(3)]
    [MessagePackObject]
    public class PingMsg : IProtocolMessage
    {
        /// <summary>客户端发送时的 Unix 毫秒时间戳。</summary>
        [Key(0)] public long ClientTimestamp { get; set; }
        /// <summary>上一次 Pong 响应中服务端返回的时间戳。</summary>
        [Key(1)] public long ServerTimestamp { get; set; }
    }

    /// <summary>
    /// 协议 ID 4：心跳探测响应消息。
    /// 服务端收到 PingMsg 后返回，客户端据此计算 RTT。
    /// </summary>
    [ProtocolId(4)]
    [MessagePackObject]
    public class PongMsg : IProtocolMessage
    {
        /// <summary>原始 PingMsg 中的客户端时间戳。</summary>
        [Key(0)] public long ClientTimestamp { get; set; }
        /// <summary>服务端回复时的 Unix 毫秒时间戳。</summary>
        [Key(1)] public long ServerTimestamp { get; set; }
    }

    /// <summary>
    /// 协议 ID 5：服务端主动发起的断开连接通知。
    /// 通常用于服务端重启或强制踢出客户端。
    /// </summary>
    [ProtocolId(5)]
    [MessagePackObject]
    public class ByebyeServerMsg : IProtocolMessage { }

    /// <summary>
    /// 协议 ID 6：客户端主动请求断开连接的通知。
    /// 服务端收到后执行清理流程。
    /// </summary>
    [ProtocolId(6)]
    [MessagePackObject]
    public class ByebyeClientMsg : IProtocolMessage { }
}
