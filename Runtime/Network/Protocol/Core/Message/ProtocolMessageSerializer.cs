using System;
using MessagePack;

namespace GoveKits.Runtime.Network
{
    public interface IProtocolMessageSerializer
    {
        byte[] Serialize<T>(T obj) where T : IProtocolMessage;
        T Deserialize<T>(byte[] data) where T : IProtocolMessage;
        IProtocolMessage Deserialize(Type type, byte[] data);
    }


    public class MessagePackSerializerAdapter : IProtocolMessageSerializer
    {
        private readonly MessagePackSerializerOptions _options;

        public MessagePackSerializerAdapter(MessagePackSerializerOptions options = null)
        {
            _options = options ?? MessagePackSerializerOptions.Standard;
        }

        public byte[] Serialize<T>(T obj) where T : IProtocolMessage
            => MessagePackSerializer.Serialize(obj, _options);

        public T Deserialize<T>(byte[] data) where T : IProtocolMessage
            => MessagePackSerializer.Deserialize<T>(data, _options);

        public IProtocolMessage Deserialize(Type type, byte[] data)
            => (IProtocolMessage)MessagePackSerializer.Deserialize(type, data, _options);
    }
}