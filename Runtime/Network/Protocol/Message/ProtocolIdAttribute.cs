using System;
using MessagePack;

namespace GoveKits.Runtime.Network
{
    public interface IProtocolMessage { }
    

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ProtocolIdAttribute : Attribute
    {
        public ushort Id { get; }  // ushort 足够，65535 个消息类型
        public ProtocolIdAttribute(ushort id) => Id = id;
    }
}