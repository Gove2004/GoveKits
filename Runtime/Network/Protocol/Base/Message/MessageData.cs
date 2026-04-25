using System;
using MessagePack;

namespace GoveKits.Runtime.Network
{
    // 基础消息接口
    public interface IProtocolMessage { }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ProtocolIdAttribute : Attribute
    {
        public ushort Id { get; }
        public ProtocolIdAttribute(ushort id) => Id = id;
    }
}