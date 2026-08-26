using System;
using MessagePack;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 协议消息基础接口。
    /// 所有网络通信的消息类型均需实现此接口，并通过 [ProtocolId] 特性标注唯一协议 ID。
    /// </summary>
    public interface IProtocolMessage { }

    /// <summary>
    /// 协议 ID 特性，用于标注消息类或结构体，为其分配唯一的整数协议标识。
    /// 该 ID 在序列化和反序列化时作为消息类型的路由依据。
    /// 只能应用于 Class 和 Struct 类型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ProtocolIdAttribute : Attribute
    {
        public ushort Id { get; }
        public ProtocolIdAttribute(ushort id) => Id = id;
    }
}
