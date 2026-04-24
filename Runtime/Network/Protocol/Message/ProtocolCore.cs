using System;
using MessagePack;
using MessagePack.Resolvers;

namespace GoveKits.Runtime.Network
{
    public static class ProtocolCore
    {
        private static readonly ProtocolMapper _mapper = new ProtocolMapper();
        private static readonly ProtocolSerializer _serializer = new ProtocolSerializer();

        public static void ScanAndRegister() => _mapper.ScanAndRegister();
        public static ushort GetId(Type type) => _mapper.GetId(type);
        public static ushort GetId<T>() => _mapper.GetId<T>();
        public static Type GetType(ushort id) => _mapper.GetType(id);

        public static void AddResolver(IFormatterResolver resolver) => _serializer.AddResolver(resolver);

        // 彻底的纯净数据序列化（不包含 ID 和 长度头）
        public static byte[] Serialize<T>(T message) where T : IProtocolMessage 
            => _serializer.Serialize(message);

        public static IProtocolMessage Deserialize(ushort id, ReadOnlyMemory<byte> purePayload)
        {
            Type type = _mapper.GetType(id);
            if (type == null) return null;
            return _serializer.Deserialize(type, purePayload);
        }
    }
}