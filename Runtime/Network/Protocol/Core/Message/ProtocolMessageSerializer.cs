using System;
using MessagePack;
using MessagePack.Resolvers;

namespace GoveKits.Runtime.Network
{
    public interface IProtocolMessageSerializer
    {
        byte[] Serialize<T>(T obj) where T : IProtocolMessage;
        T Deserialize<T>(byte[] data) where T : IProtocolMessage;
        IProtocolMessage Deserialize(Type type, byte[] data);
    }

    public abstract class CustomResolverMessagePackSerializer: IProtocolMessageSerializer
    {
        protected readonly MessagePackSerializerOptions _options;

        protected CustomResolverMessagePackSerializer(IFormatterResolver resolver)
        {
            _options = MessagePackSerializerOptions.Standard
                .WithResolver(resolver);
        }

        public byte[] Serialize<T>(T obj) where T : IProtocolMessage
            => MessagePackSerializer.Serialize(obj, _options);

        public T Deserialize<T>(byte[] data) where T : IProtocolMessage
            => MessagePackSerializer.Deserialize<T>(data, _options);

        public IProtocolMessage Deserialize(Type type, byte[] data)
            => (IProtocolMessage)MessagePackSerializer.Deserialize(type, data, _options);
    }


    public class StandardMessagePackSerializer : CustomResolverMessagePackSerializer
    {
        public StandardMessagePackSerializer()
            : base(StandardResolver.Instance)
        {
        }
    }
}