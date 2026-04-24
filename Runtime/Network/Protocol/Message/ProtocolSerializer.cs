using System;
using MessagePack;
using MessagePack.Resolvers;

namespace GoveKits.Runtime.Network
{
    internal class ProtocolSerializer
    {
        private MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard;

        public void AddResolver(IFormatterResolver resolver)
        {
            _options = _options.WithResolver(CompositeResolver.Create(resolver, _options.Resolver));
        }

        public byte[] Serialize<T>(T message) where T : IProtocolMessage
        {
            return MessagePackSerializer.Serialize(message, _options);
        }

        public IProtocolMessage Deserialize(Type type, ReadOnlyMemory<byte> purePayload)
        {
            return (IProtocolMessage)MessagePackSerializer.Deserialize(type, purePayload, _options);
        }
    }
}